using System.Linq;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
namespace Prometheus.Scraper
{
    using System;
    using System.Collections.Generic;

    public class Sample
    {
        public string Name { get; }
        public double Value { get; }
        public IDictionary<string, string> Labels { get; }
        public long? Timestamp { get; }

        public Sample(string name, double value, IDictionary<string, string> labels, long? timestamp)
        {
            Name = name;
            Value = value;
            Labels = labels;
            Timestamp = timestamp;
        }
    }

    internal class MetricFamilyBuilder
    {
        private string _name;
        private string _type;
        private string _help;
        private List<Sample> _samples = new List<Sample>();

        public MetricFamilyBuilder(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void SetType(string type)
        {
            _type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public void SetHelp(string help)
        {
            _help = help ?? throw new ArgumentNullException(nameof(help));
        }

        public void AddSample(string name, string labels, string stringValue)
        {
            var labelDict = ParseLabels(labels);
            var parts = stringValue.Split(' ');
            var value = ParseValue(parts[0]);
            long? timestamp = null;
            if (parts.Length == 2)
            {
                timestamp = ParseTimestamp(parts[1]);
            }
            var sample = new Sample(name, value, labelDict, timestamp);
            _samples.Add(sample);
        }

        private long ParseTimestamp(string stringValue)
        {
            return long.Parse(stringValue);
        }

        private readonly Regex LabelRegex = new Regex("(?<key>[a-zA-Z_:][a-zA-Z0-9_:]*)=\"(?<value>[^\"]*)\"", RegexOptions.Compiled);
        private IDictionary<string, string> ParseLabels(string labels)
        {
            return LabelRegex
                    .Matches(labels)
                    .Cast<Match>()
                    .ToDictionary(x => x.Groups["key"].Value, 
                                  x => x.Groups["value"].Value);
        }

        // value is a float represented as required by Go's ParseFloat() function.
        // In addition to standard numerical values, Nan, +Inf, and -Inf are valid values
        // representing not a number, positive infinity, and negative infinity, respectively.
        private double ParseValue(string stringValue)
        {
            if (stringValue == "Nan") return double.NaN;
            if (stringValue == "+Inf") return double.MaxValue;
            if (stringValue == "-Inf") return double.MinValue;
            return double.Parse(stringValue);
        }

        internal MetricFamily Build()
        {
            var metrics = new List<Metric>();
            switch(_type)
            {
                case "summary":
                    metrics.AddRange(ToSummaries(_samples));
                    break;
                case "counter":
                    metrics.AddRange(ToCounters(_samples));
                    break;
                case "gauge":
                    metrics.AddRange(ToGauges(_samples));
                    break;
                case "histogram":
                    metrics.AddRange(ToHistograms(_samples));
                    break;
                default:
                    metrics.AddRange(ToUntyped(_samples));
                    break;
            }
            if (!Enum.TryParse<MetricType>(_type, true, out var typeAsEnum))
            {
                throw new InvalidOperationException($"Unknown metric type: {_type}");
            }
            return new MetricFamily(_name, typeAsEnum, _help, metrics);
        }

        private IList<Metric> ToSummaries(IList<Sample> samples)
        {
            return new List<Metric>();
        }

        private IList<Metric> ToCounters(IList<Sample> samples)
        {
            return samples.Select(x => {
                return new Counter(x.Labels, x.Value);
            })
            .Cast<Metric>()
            .ToList();
        }

        private IList<Metric> ToGauges(IList<Sample> samples)
        {
            return samples.Select(x => {
                return new Gauge(x.Labels, x.Value);
            })
            .Cast<Metric>()
            .ToList();
        }

        private IList<Metric> ToHistograms(IList<Sample> samples)
        {
            var sumSample = samples.Single(x => x.Name.EndsWith("_sum"));
            var countSample = samples.Single(x => x.Name.EndsWith("_count"));
            
            return new List<Metric>();
        }

        private IList<Metric> ToUntyped(IList<Sample> samples)
        {
            return new List<Metric>();
        }

        readonly string[] SuffixesToStrip = new string[] 
        {
            "_sum",
            "_count",
            "_bucket",
            "_total"
        };
        internal bool IsNewFamily(string name)
        {
            if ((_type == "summary" ||
                _type == "histogram") &&
                SuffixesToStrip.Any(x => name.EndsWith(x)))
            {
                name = name.Substring(0, name.LastIndexOf('_'));
            }
            return _name != name;
        }
    }
}