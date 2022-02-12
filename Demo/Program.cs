#define OTLPExporter
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Demo
{
    public class Program
    {
        private const string MeterLibName = "Cyracom.CSharpPOC";
        private const string ServiceName = "CsharpPOC";
        private const string ServiceVersion = "1.0.0";
        public static readonly ActivitySource MyActivitySource = new ActivitySource(ServiceName, ServiceVersion);

        private static readonly Meter MyMeter = new Meter(MeterLibName, "1.0");

        static void SetupActivityListener()
        {
            ActivityListener activityListener = new ActivityListener
            {
                ShouldListenTo = s => true,
                SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData,
                Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData
            };

            ActivitySource.AddActivityListener(activityListener);
        }

        static void Main(string[] args)
        {
            //GenerateMetrics();
            GenerateTraces();

            Console.WriteLine("Press something to exit the program");
            Console.ReadLine();
        }

        private static void GenerateMetrics()
        {
#if OTLPExporter
            var meterProvider = Sdk.CreateMeterProviderBuilder()
                                    .AddMeter(MeterLibName)
                                    .AddOtlpExporter(SetExporterOptions)
                                    .Build();
#else
            var meterProvider = Sdk.CreateMeterProviderBuilder()
                                    .AddMeter(MeterLibName)
                                    .AddConsoleExporter()
                                    .Build();

#endif
            SetupActivityListener();
            var counterExceptions = MyMeter.CreateCounter<long>("exceptions", null, "number of exceptions caught");

            var exceptionTypeparam = new KeyValuePair<string, object>("exception_type", "FileLoadException");
            var handledParam = new KeyValuePair<string, object>("handled_by_user", true);

            counterExceptions.Add(30, exceptionTypeparam, handledParam);
            /*
            MyFruitCounter.Add(1, new ("name", "apple"), new ("color", "red"));
            MyFruitCounter.Add(2, new ("name", "lemon"), new ("color", "yellow"));
            MyFruitCounter.Add(1, new ("name", "lemon"), new ("color", "yellow"));
            MyFruitCounter.Add(2, new ("name", "apple"), new ("color", "green"));
            MyFruitCounter.Add(5, new ("name", "apple"), new ("color", "red"));
            MyFruitCounter.Add(4, new ("name", "lemon"), new ("color", "yellow")); */
        }

        private static void GenerateTraces()
        {
#if OTLPExporter
            TracerProvider tracerProvider = Sdk.CreateTracerProviderBuilder()
                                    .AddSource(ServiceName)
                                    .SetResourceBuilder(
                                            ResourceBuilder.CreateDefault()
                                                .AddService(serviceName: ServiceName, serviceVersion: ServiceVersion))
                                    .AddHttpClientInstrumentation(SetInstrumentationOptions)
                                    .AddOtlpExporter(SetExporterOptions)
                                    .Build();
#else
            var tracerProvider = Sdk.CreateTracerProviderBuilder()
                                    .AddSource(ServiceName)
                                    .SetResourceBuilder(
                                             ResourceBuilder.CreateDefault()
                                                 .AddService(serviceName: ServiceName, serviceVersion: ServiceVersion))
                                    .AddHttpClientInstrumentation(SetInstrumentationOptions)
                                    .AddConsoleExporter()
                                    .Build();
#endif
            SetupActivityListener();

            // string proxyHost = "wavefront.proxy.hostname";
            // int metricsPort = 2878;
            using (Activity myActivity = MyActivitySource.StartActivity("Activity Test", ActivityKind.Client))
            {
                ActivityTest(10);
            }

            using (Activity myActivity = MyActivitySource.StartActivity("Net instrumentation Test", ActivityKind.Client))
            {
                HttpCaller.Start(100000);
            }
        }

        private static void SetExporterOptions(OtlpExporterOptions options)
        {
            // if running collector on the same box as CFE Endpoint below should be local host
            // The default value of endpoint is http://localhost:4317.
            //options.Endpoint = new Uri("http://0.0.0.0:4318");
            options.Protocol = OtlpExportProtocol.Grpc;
        }

        private static void ActivityTest(int iterCount)
        {
            for (int i = 0; i < iterCount; ++i)
            {
                Console.WriteLine($"Iter:{i}");
                using (Activity childActivity = Program.MyActivitySource.StartActivity("Operation Activity", ActivityKind.Internal))
                {
                    childActivity.AddTag("Iteration", i);
                    if (i % 3 == 0)
                    {
                        childActivity.SetTag("otel.status_code", "ERROR");
                        childActivity.SetTag("otel.status_description", $"iter {i} failed");
                    }
                }
            }
        }

        private static void SetInstrumentationOptions(HttpWebRequestInstrumentationOptions options)
        {
            options.SetHttpFlavor = true;
            options.RecordException = true;
        }
    }
}
