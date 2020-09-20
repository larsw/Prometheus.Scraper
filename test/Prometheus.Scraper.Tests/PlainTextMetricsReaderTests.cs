using System.Linq;
using System.IO;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Prometheus.Scraper.Tests
{
    public class PlainTextMetricsReaderTests
    {
        private readonly ITestOutputHelper _output;

        public PlainTextMetricsReaderTests(Xunit.Abstractions.ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Test1()
        {
            using (var streamReader = File.OpenText("TestCases/metrics1.txt"))
            {
                var metricFamilyReader = new PlainTextMetricsReader(streamReader);
                var metricFamilies = metricFamilyReader.ReadAllFamilies().ToList();
                // // compare to grep "# TYPE" metrics1.txt | cut -d " " -f 3 > names.txt
                // using (var file = File.CreateText("found-names.txt"))
                // {
                //     file.Write(string.Join('\n', metricFamilies.Select(x => x.Name)));
                //     file.WriteLine();
                //     file.Flush();
                //     file.Close();
                // }
                _output.WriteLine($"Metric family count: {metricFamilies.Count()}");
                _output.WriteLine($"First metric family name: {metricFamilies.First().Name}");
                _output.WriteLine($"Last metric family name: {metricFamilies.Last().Name}");
            }
        }
    }
}
