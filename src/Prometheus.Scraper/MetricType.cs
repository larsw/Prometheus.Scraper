using System;

namespace Prometheus.Scraper
{
    public enum MetricType
    {
        Untyped,
        Counter,
        Gauge,
        Summary,
        Histogram
    }
}
