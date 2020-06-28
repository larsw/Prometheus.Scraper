using System.Linq;
using System.IO;
using System;
using Xunit;

namespace Prometheus.Scraper.Tests
{
    public class PlainTextMetricsReaderTests
    {
        [Fact]
        public void Test1()
        {
            using (var streamReader = File.OpenText("TestCases/metrics1.txt"))
            {
                var metricFamilyReader = new PlainTextMetricsReader(streamReader);
                var metricFamilies = metricFamilyReader.ReadAllFamilies();
                Assert.True(metricFamilies.Any());
            }
        }
    }
}
