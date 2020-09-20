using System.Collections.ObjectModel;
namespace Prometheus.Scraper
{
    using System;
    using System.Collections.Generic;

    public class MetricFamily
    {
        public string Name { get; }
        public MetricType Type { get; }
        public string Help { get; }
        public IReadOnlyCollection<Metric> Metrics { get; }

        public MetricFamily(string name, MetricType type, string help, IList<Metric> metrics)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Help = help;
            Type = type;
            Metrics = new ReadOnlyCollection<Metric>(metrics);
        }
    }

    public abstract class Metric
    {
        IReadOnlyDictionary<string, string> Labels { get; }

        protected Metric()
        {
            Labels = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        }

        protected Metric(IDictionary<string, string> labels)
        {
            Labels = new ReadOnlyDictionary<string, string>(labels);
        }
    }
    public class Counter : Metric
    {
        public double Value { get; }

        public Counter(IDictionary<string, string> labels, double value)
            : base(labels)
        {
            Value = value;
        }
    }

    public class Summary : Metric
    {
        public Summary(IDictionary<string, string> labels)
            : base(labels)
        {
            
        }
    }

    public class Histogram : Metric
    {
        public Histogram(IDictionary<string, string> labels)
            : base(labels)
        {

        }
        // Buckets
    }

    public class Gauge : Metric
    {
        public double Value { get; }
        public Gauge(IDictionary<string, string> labels, double value)
            :base(labels)
        {
            Value = value;
        }
    }

    public class Untyped : Metric
    {
        public Untyped(IDictionary<string, string> labels, IList<Sample> samples)
            :base(labels)
        {

        }
    }
}