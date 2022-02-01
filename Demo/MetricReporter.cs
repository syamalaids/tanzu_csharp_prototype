using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Reporting.Wavefront.Builder;
using App.Metrics.Scheduling;
using Wavefront.SDK.CSharp.Common;
using Wavefront.SDK.CSharp.Proxy;

namespace Demo
{
    public static class MetricReporter
    {
        public static void Start(string proxyHost, int metricsPort)
        {
            IWavefrontSender wavefrontProxyClient = new WavefrontProxyClient.Builder(proxyHost)
                .MetricsPort(metricsPort)
                .Build();

            IMetricsBuilder metricsBuilder = new MetricsBuilder();
            metricsBuilder.Configuration.Configure(options =>
            {
                options.DefaultContextLabel = "service";
                options.GlobalTags = new GlobalMetricTags(new Dictionary<string, string>
                    {
                        { "dc", "us-west-2" },
                        { "env", "staging" }
                    });
            });

            metricsBuilder.Report.ToWavefront(options =>
            {
                options.WavefrontSender = wavefrontProxyClient;
                options.Source = "csharp-demo.cyracom.com";
            });

            IMetricsRoot metrics = metricsBuilder.Build();
            CounterOptions evictions = new CounterOptions
            {
                Name = "cache-evictions"
            };

            metrics.Measure.Counter.Increment(evictions);

            var scheduler = new AppMetricsTaskScheduler(TimeSpan.FromSeconds(5), async () =>
            {
                await Task.WhenAll(metrics.ReportRunner.RunAllAsync());
            });

            scheduler.Start();
        }
    }
}
