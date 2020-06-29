using System.Text.RegularExpressions;
using System.IO;
using System;
using System.Collections.Generic;

namespace Prometheus.Scraper
{
    public class PlainTextMetricsReader : IDisposable
    {
        private readonly TextReader _reader;
        public PlainTextMetricsReader(TextReader reader)
        {
            _reader = reader;
        }

        private Regex HelpRegex = new Regex("^#\\s+HELP\\s+(?<name>[a-zA-Z_:][a-zA-Z0-9_:]*)\\s+(?<value>.*)", RegexOptions.Compiled);
        private MetricFamily IsHelp(ref MetricFamilyBuilder builder, string line)
        {
            var match = HelpRegex.Match(line);
            if (!match.Success)
            {
                return null;
            }
            var name = match.Groups["name"].Value;
            var value = match.Groups["value"].Value;
            if (builder == null)
            {
                builder = new MetricFamilyBuilder(name);
            }
            else if (builder.IsNewFamily(name))
            {
                var metricFamily = builder.Build();
                builder = new MetricFamilyBuilder(name);
                builder.SetHelp(value);
                return metricFamily;
            }
            builder.SetHelp(value);
            return null;
        }

        private Regex TypeRegex = new Regex("^#\\s+TYPE\\s+(?<name>[a-zA-Z_:][a-zA-Z0-9_:]*)\\s+(?<value>.*)", RegexOptions.Compiled);
        private MetricFamily IsType(ref MetricFamilyBuilder builder, string line)
        {
            var match = TypeRegex.Match(line);
            if (!match.Success)
            {
                return null;
            }
            var name = match.Groups["name"].Value;
            var value = match.Groups["value"].Value;
            if (builder == null)
            {
                builder = new MetricFamilyBuilder(name);
            }
            if (builder.IsNewFamily(name))
            {
                var metricFamily = builder.Build();
                builder = new MetricFamilyBuilder(name);
                builder.SetType(value);
                return metricFamily;
            }
            builder.SetType(value);
            return null;
        }

        private Regex CommentOrWhitespaceRegex = new Regex("(^#.*|\\s*)", RegexOptions.Compiled);
        public bool IsCommentOrWhitespace(string line)
        {
            return CommentOrWhitespaceRegex.IsMatch(line);
        }


        private Regex SampleRegex = new Regex("(?<name>[a-zA-Z_:][a-zA-Z0-9_:]*)(?<labels>\\{.*\\})?\\s+(?<value>.*)", RegexOptions.Compiled);
        private MetricFamily IsSample(ref MetricFamilyBuilder builder, string line)
        {
            var match = SampleRegex.Match(line);
            if (!match.Success)
            {
                return null;
            }

            var name = match.Groups["name"].Value;
            var labels = match.Groups["labels"].Value;
            var value = match.Groups["value"].Value;
            
            if (builder == null)
            {
                builder = new MetricFamilyBuilder(name);
            }
            if (builder.IsNewFamily(name))
            {
                var metricFamily = builder.Build();
                builder = new MetricFamilyBuilder(name);
                builder.AddSample(labels, value);
                return metricFamily;
            }
            builder.AddSample(labels, value);
            return null;
        }

        public IEnumerable<MetricFamily> ReadAllFamilies()
        {
            string line;
            MetricFamilyBuilder builder = null;
            while ((line = _reader.ReadLine()) != null)
            {
                line = line.Trim();
                var metricFamily = IsHelp(ref builder, line);
                if (metricFamily != null) 
                {
                    yield return metricFamily;
                } 
                else
                {
                    metricFamily = IsType(ref builder, line);
                    if (metricFamily != null) {
                        yield return metricFamily;
                    }
                    else
                    {
                        if (IsCommentOrWhitespace(line))
                        {
                            continue;
                        }
                        metricFamily = IsSample(ref builder, line);
                        if (metricFamily != null)
                        {
                            yield return metricFamily;
                        }
                        else
                        {
                            // dead end.
                            throw new InvalidOperationException("Uh oh. We shouldn't be here.");
                        }
                    }
                }    
            }
            yield return builder.Build();
        }

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _reader.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}