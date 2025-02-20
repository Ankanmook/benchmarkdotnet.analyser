#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.BuildServer.GitHubActions
nuget Fake.Core.Target //"
#if !FAKE
  #load "./.fake/fakebuild.fsx/intellisense.fsx"
#endif

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet

let combine x y = System.IO.Path.Combine(x,y)

// Build variables
let sln = "benchmarkdotnetanalyser.sln"
let mainProj = ".\src\BenchmarkDotNetAnalyser\BenchmarkDotNetAnalyser.csproj"
let publishDir = "publish"
let unitTestDir = "test/BenchmarkDotNetAnalyser.Tests.Unit"
let integrationTestDir = "test/BenchmarkDotNetAnalyser.Tests.Integration"
let integrationTestResultsDir = "BenchmarkDotNetResults"
let sampleBenchmarksDir = "test/BenchmarkDotNetAnalyser.SampleBenchmarks/bin/Release/net5.0"
let sampleBenchmarksResults = "BenchmarkDotNet.Artifacts/results"
let sampleBenchmarksResultsDir = combine sampleBenchmarksDir sampleBenchmarksResults

let unitTestResultsOutputDir = unitTestDir + "/TestResults"
let integrationTestResultsOutputDir = integrationTestDir + "/TestResults"
let strykerOutputDir = unitTestDir + "/StrykerOutput"

let strykerBreak = 69
let strykerHigh = 80
let strykerLow = 70

let runNumber = (match Fake.BuildServer.GitHubActions.Environment.CI false with
                    | true -> Fake.BuildServer.GitHubActions.Environment.RunNumber
                    | _ -> "0")
let commitSha = Fake.BuildServer.GitHubActions.Environment.Sha
let versionSuffix = match Fake.BuildServer.GitHubActions.Environment.Ref with
                    | null 
                    | "refs/heads/main" ->  ""
                    | _ ->                  "-preview"

let version =  sprintf "0.1.%s%s" runNumber versionSuffix
let infoVersion = match commitSha with
                    | null -> version
                    | sha -> sprintf "%s.%s" version sha

sprintf "Ref: %s" Fake.BuildServer.GitHubActions.Environment.Ref |> Trace.log
sprintf "Version: %s" version |> Trace.log
sprintf "Info Version: %s" infoVersion |> Trace.log

let assemblyInfoParams (buildParams)=
    [ ("Version", version); ("AssemblyInformationalVersion", infoVersion) ] |> List.append buildParams   

let packBuildParams (buildParams) =
    [ ("PackageVersion", version); ] |> List.append buildParams   
    
let codeCoverageParams (buildParams)=
    [   ("CollectCoverage", "true"); 
        ("CoverletOutput", "./TestResults/coverage.info"); 
        ( "CoverletOutputFormat", "lcov") ]  |> List.append buildParams

let buildOptions (opts: DotNet.BuildOptions) =            
    { opts with Configuration = DotNet.BuildConfiguration.Release;
                    MSBuildParams = { opts.MSBuildParams with Properties = assemblyInfoParams opts.MSBuildParams.Properties; WarnAsError = Some [ "*" ] } }

let testOptions (opts: DotNet.TestOptions)=
    { opts with NoBuild = true; 
                    Configuration = DotNet.BuildConfiguration.Release; 
                    Logger = Some "trx;LogFileName=test_results.trx";
                    MSBuildParams = { opts.MSBuildParams with Properties = codeCoverageParams opts.MSBuildParams.Properties } }

let packOptions(opts: DotNet.PackOptions)=
    { opts with Configuration = DotNet.BuildConfiguration.Release;
                MSBuildParams = { opts.MSBuildParams with Properties = (packBuildParams opts.MSBuildParams.Properties |> assemblyInfoParams )};
                OutputPath = sprintf ".\\%s\\toolpackage" publishDir |> Some;
        }

let publishOptions(runtime: string)(opts: DotNet.PublishOptions)= 
    { opts with 
       SelfContained = Some true;
       Runtime = Some runtime;       
       OutputPath = Some (sprintf ".\%s\%s" publishDir runtime;);
       MSBuildParams = { opts.MSBuildParams with Properties = assemblyInfoParams opts.MSBuildParams.Properties}
     }

// Declare build targets
Target.create "Clean" (fun _ ->  
  Fake.IO.Directory.delete publishDir
  Fake.IO.Directory.create publishDir
  Fake.IO.Directory.delete unitTestResultsOutputDir
  Fake.IO.Directory.delete integrationTestResultsOutputDir
  Fake.IO.Directory.delete strykerOutputDir
)

Target.create "Restore" (fun _ ->    
  DotNet.restore id sln
)

Target.create "Build" (fun _ ->
  DotNet.build buildOptions sln 
)

Target.create "Unit Tests" (fun _ ->
  let proj = combine unitTestDir "BenchmarkDotNetAnalyser.Tests.Unit.csproj"
  DotNet.test testOptions proj
)

Target.create "Package dotnet tool" (fun _ -> 
  DotNet.pack packOptions mainProj
)

Target.create "Consolidate code coverage" (fun _ ->  
  let args = sprintf @"-reports:""./test/**/coverage.info"" -targetdir:""./%s/codecoverage"" -reporttypes:""HtmlSummary""" publishDir
  let result = DotNet.exec id "reportgenerator" args
  
  if not result.OK then failwithf "reportgenerator failed!"  
)

Target.create "Stryker" (fun _ ->  
  let opts (o: DotNet.Options) = { o with WorkingDirectory = unitTestDir }

  let args = sprintf "-th %i -tl %i -tb %i" strykerHigh strykerLow strykerBreak
  let result = DotNet.exec opts "dotnet-stryker" args

  if not result.OK then failwithf "Stryker failed!"  

  
  let strykerFiles = !! (strykerOutputDir + "/**/mutation-report.html") 
  let strykerTargetPath = "stryker" |> combine publishDir
  
  strykerFiles |> Fake.IO.Shell.copy strykerTargetPath
)

Target.create "Run Sample benchmarks" (fun _ ->
  
  let opts (o: DotNet.Options) = { o with WorkingDirectory = sampleBenchmarksDir }  
  let args = "-f *"
  let result = DotNet.exec opts "BenchmarkDotNetAnalyser.SampleBenchmarks.dll" args

  if not result.OK then failwithf "Sample benchmarks failed!"
)

Target.create "Copy benchmark results" (fun _ -> 

  let sourcePath = combine __SOURCE_DIRECTORY__ sampleBenchmarksResultsDir
  let targetPath = integrationTestResultsDir |> combine integrationTestDir |> combine __SOURCE_DIRECTORY__
      
  Trace.log sourcePath
  Trace.log targetPath

  !! (sourcePath + "/*.csv") |> Fake.IO.Shell.copy targetPath
  !! (sourcePath + "/*-report-full.json") |> Fake.IO.Shell.copy targetPath 
)

Target.create "Integration Tests" (fun _ ->
  let proj = combine integrationTestDir "BenchmarkDotNetAnalyser.Tests.Integration.csproj"

  DotNet.test testOptions proj
)

// Declare build dependencies
"Clean"
  ==> "Restore"
  ==> "Build"
  ==> "Unit Tests"
  ==> "Integration Tests"
  ==> "Consolidate code coverage"
  ==> "Stryker"
  ==> "Package dotnet tool"

"Build"
  ==> "Run Sample benchmarks" 
  ==> "Copy benchmark results"
  
Target.runOrDefaultWithArguments  "Package dotnet tool"