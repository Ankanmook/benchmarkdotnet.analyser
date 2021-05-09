![Build](https://github.com/NewDayTechnology/benchmarkdotnet.analyser/actions/workflows/actions_buildtestpackage.yml/badge.svg)
 [![Contributor Covenant](https://img.shields.io/badge/Contributor%20Covenant-2.0-4baaaa.svg)](code_of_conduct.md)

# BenchmarkDotNet Analyser

## Description
A tool for analysing [BenchmarkDotNet](https://benchmarkdotnet.org/) results.![Front](./docs/cli_front.png)

BDNA collects, aggregates and analyses [BenchmarkDotNet](https://benchmarkdotnet.org/) results for performance degredations. If you want to ensure your critical code builds have acceptable performance, BDNA may help you.

---

### What BDNA needs from BenchmarkDotNet

BDNA analyses the full JSON report files from [BenchmarkDotNet](https://benchmarkdotnet.org/). These files normally have the suffix ``-report-full.json`` and are generated by benchmarks with ``JsonExporterAttribute.Full`` attribute. BDNA ignores the other files from [BenchmarkDotNet](https://benchmarkdotnet.org/). See [Exporters](https://benchmarkdotnet.org/articles/configs/exporters.html) for more information.

---

### How BDNA collects data

BDNA collects [BenchmarkDotNet](https://benchmarkdotnet.org/) full JSON report files. These files are usually found under ``./BenchmarkDotNet.Artifacts/results`` folders. BDNA will aggregate these results into a single dataset (in reality a local disk folder) for later analysis. 

To aggregate, use ``bdna aggregate``, giving the folder containing new results (``--new``), the folder containing a previous aggregated dataset (``--aggregates``), and the folder for the new dataset (``--output``). 


```
Aggregate benchmark results into a single dataset.

Usage: bdna aggregate [options]

Options:
  -new|--new <NEW_BENCHMARKS_PATH>                     The path containing new benchmarks results.
  -aggs|--aggregates <AGGREGATED_BENCHMARKS_PATH>      The path containing the dataset to roll into.
  -out|--output <OUTPUT_AGGREGATES_PATH>               The path for the new dataset.
  -runs|--runs <BENCHMARK_RUNS>                        The number of benchmark runs to keep when aggregating.
  -datafilesuffix|--datafilesuffix <DATA_FILE_SUFFIX>  The file name suffix for data files. Optional. Default:
                                                       -report-full.json
  -build|--build <BUILD_URI>                           The new build's URL. Optional.
  -branch|--branch <BRANCH_NAME>                       The new build's branch name. Optional.
  -t|--tag <TAGS>                                      A tag for the new build. Optional.
  -v|--verbose                                         Emit verbose logging.
  -?|-h|--help                                         Show help information.
```

``--new``: the folder containing a new benchmark run to fold into the aggregate dataset. This is typically the ``./BenchmarkDotNet.Artifacts/results`` folder.

``--aggregates``: the folder containing previously aggregated data. 

``--output``: the folder for the new dataset. The aggregates from ``--aggregates`` and the new results from ``--new`` will be placed into the ``--output`` path. Both the ``--aggregates`` and ``--output`` arguments can be the same.

``--runs``: the maximum number of benchmark results that are aggregated in ``--output``. 

``--datafilesuffix``: the file name extension of files to aggregate. The default is ``-report-full.json``.

``--build``: the new build's URL.

``--branch``: the new build's branch name.

``--tag``: arbitrary tags to assign to the new benchmark run. Multiple tags can be assigned to the run.

---

### How BDNA analyses benchmarks

Use ``bdna analyse`` to scan the aggregate dataset.

Every benchmark run (that is, the benchmark type, method and parameters) is analysed in isolation. For example, if your benchmarks have runs ``Dijkstra(Depth=1)`` & ``Dijkstra(Depth=2)`` each of these will be analysed independently, and not ``Dijkstra()`` as a whole.

From all the aggregated runs, BDNA picks the minimum mean value per run as the baseline value. 

The benchmark run that was added last is taken as the comparand: if this latest value is within your tolerances the analysis will pass, if not the analysis fails. 


```
Analyse a benchmark dataset for performance degradation.

Usage: bdna analyse [options]

Options:
  -tol|--tolerance <TOLERANCE>          Tolerance of errors from baseline performance.
  -max|--maxerrors <MAX_ERRORS>         The maximum number of failures to tolerate.
  -stat|--statistic <STATISTIC>         The result statistic to analyse.
  -aggs|--aggregates <AGGREGATES_PATH>  The path of the aggregated dataset to analyse.
  -v|--verbose                          Emit verbose logging.
  -?|-h|--help                          Show help information.
```

``--aggregates``: the folder containing the aggregated dataset. This is the same as the ``--output`` parameter from ``bdna aggregate``.

``--tolerance``: the percentage deviance from the minimum value. 

``--maxerrors``: the maximum number of errors for the analysis to pass. If this is 0 then any error will cause the analysis to fail.

``--statistic``: the statistic value, for each run, to use. By default this is ``Mean`` with ``Min``, ``Max`` & ``Median``.


If there are no degradations in performance, ``bdna analyse`` will return a return code of 0, otherwise 1 will be returned. This is what you should watch to fail builds upon degraded performance.

---

## Technologies used
We use C# and .NET Core 5.0 for this project.

Some 3rd party packages that we depend on:-
* [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/)
* [Nate McMaster's Command line utils](https://www.nuget.org/packages/McMaster.Extensions.CommandLineUtils)
* [Microsoft's Dependency Injection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)
* [Crayon](https://www.nuget.org/packages/Crayon/)
* [FluentAssertions](https://www.nuget.org/packages/FluentAssertions)
* [FsCheck](https://www.nuget.org/packages/FsCheck.Xunit)
* [NSubstitute](https://www.nuget.org/packages/NSubstitute/)
* [Coverlet](https://www.nuget.org/packages/coverlet.collector/)
* [Xunit](https://www.nuget.org/packages/xunit/)
* [Stryker](https://stryker-mutator.io/docs/stryker-net/Introduction/)
* [FAKE](https://fake.build/)

---

## Building 

The local build scripts are a [FAKE build script](FakeBuild.fsx) and a [Powershell bootstrapper](build.ps1). Reference [FAKE](https://fake.build/) for build target selection.

---

## More reading

[License](LICENSE)

[Copyright notice](NOTICE)

[How to Contribute](CONTRIBUTING.md)

[Our Code of Conduct](CODE_OF_CONDUCT.md)

[Security notes](SECURITY.md)

[How we support this project](SUPPORT.md)
