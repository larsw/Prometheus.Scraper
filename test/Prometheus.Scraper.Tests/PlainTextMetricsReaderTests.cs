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
                var metricFamilies = metricFamilyReader.ReadAllFamilies();
                Assert.True(metricFamilies.Any());
                _output.WriteLine($"Metric family count: {metricFamilies.Count()}");
            }
        }
    }
}
