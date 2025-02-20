﻿using System;
using System.Linq;
using BenchmarkDotNetAnalyser.IO;

namespace BenchmarkDotNetAnalyser.Reporting
{
    public class ReporterProvider : IReporterProvider
    {
        private readonly ICsvFileWriter _csvFileWriter;
        private readonly IBenchmarkReader _benchmarkReader;

        public ReporterProvider(ICsvFileWriter csvFileWriter, IBenchmarkReader infoReader)
        {
            _csvFileWriter = csvFileWriter.ArgNotNull(nameof(csvFileWriter));
            _benchmarkReader = infoReader.ArgNotNull(nameof(infoReader));
        }

        public IBenchmarksReportGenerator GetReporter(string kind)
        {
            var value = Enum.GetValues<ReportKind>()
                .FirstOrDefault(e => StringComparer.InvariantCultureIgnoreCase.Equals(kind, e.ToString()));

            return GetReporter(value);
        }

        public IBenchmarksReportGenerator GetReporter(ReportKind kind)
        {
            return (kind switch
            {
                ReportKind.Csv => new CsvBenchmarksReportGenerator(_csvFileWriter, _benchmarkReader),
                _ => throw new InvalidOperationException($"Unrecognised kind: {kind}")
            });

        }
    }
}
