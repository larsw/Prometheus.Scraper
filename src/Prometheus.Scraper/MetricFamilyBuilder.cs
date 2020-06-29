using System.Linq;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
namespace Prometheus.Scraper
{
    using System;
    using System.Collections.Generic;

    public class Sample
    {
        public double Value {get;}
        public IReadOnlyDictionary<string, string> Labels { get; }
        public long? Timestamp { get; }

        public Sample(double value, IDictionary<string, string> labels, long? timestamp)
        {
            Value = value;
            Labels = new ReadOnlyDictionary<string, string>(labels);
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

        public void AddSample(string labels, string stringValue)
        {
            var labelDict = ParseLabels(labels);
            var parts = stringValue.Split(' ');
            var value = ParseValue(parts[0]);
            long? timestamp = null;
            if (parts.Length == 2)
            {
                timestamp = ParseTimestamp(parts[1]);
            }
            var sample = new Sample(value, labelDict, timestamp);
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
            return new MetricFamily(_name, _type, _help, _samples);
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