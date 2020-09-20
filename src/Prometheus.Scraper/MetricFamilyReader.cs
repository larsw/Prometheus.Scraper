namespace Prometheus.Scraper
{
    using System;
    using System.Text.RegularExpressions;
    using System.IO;
    using System.Collections.Generic;

    public class PlainTextMetricsReader : IDisposable
    {
        private static readonly Regex HelpRegex = new Regex("^#\\s+HELP\\s+(?<name>[a-zA-Z_:][a-zA-Z0-9_:]*)\\s+(?<value>.*)", RegexOptions.Compiled);
        private static readonly Regex TypeRegex = new Regex("^#\\s+TYPE\\s+(?<name>[a-zA-Z_:][a-zA-Z0-9_:]*)\\s+(?<value>.*)", RegexOptions.Compiled);
        private static readonly Regex SampleRegex = new Regex("(?<name>[a-zA-Z_:][a-zA-Z0-9_:]*)(?<labels>\\{.*\\})?\\s+(?<value>.*)", RegexOptions.Compiled);
        private static readonly Regex CommentOrWhitespaceRegex = new Regex("(^#.*|\\s*)", RegexOptions.Compiled);

        private readonly TextReader _reader;
        private bool _disposed;

        public PlainTextMetricsReader(TextReader reader)
        {
            _reader = reader;
        }

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

        public bool IsCommentOrWhitespace(string line)
        {
            return CommentOrWhitespaceRegex.IsMatch(line);
        }

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
                builder.AddSample(name, labels, value);
                return metricFamily;
            }
            builder.AddSample(name, labels, value);
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
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}