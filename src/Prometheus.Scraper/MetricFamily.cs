namespace Prometheus.Scraper
{
    using System;
    using System.Collections.Generic;

    public class MetricFamily
    {
        public string Name { get; }
        public MetricType Type { get; }
        public string Help { get; }
        public IReadOnlyList<Sample> Samples { get; }

        public MetricFamily(string name, string type, string help, List<Sample> samples)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MetricType typeEnum = MetricType.Untyped;
            if (type != null &&
                !Enum.TryParse<MetricType>(type, true, out typeEnum))
            {
                throw new ArgumentException($"Unknown metric type: '{type}'", nameof(type));
            }
            Type = typeEnum;
            Help = help;
            Samples = samples.AsReadOnly();
        }
    }
}