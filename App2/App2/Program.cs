// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;

namespace App2;

 class Program
{
    private static readonly ActivitySource Activity = new(nameof(Program));
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    public static async  Task Main(string[] args)
    {
       

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
        Sdk.CreateTracerProviderBuilder()
            .AddHttpClientInstrumentation()
            .AddSource(nameof(Program))
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("app2"))
            // .AddJaegerExporter(opts =>
            // {
            //     opts.Protocol = JaegerExportProtocol.HttpBinaryThrift;
            //     opts.Endpoint = new Uri("http://localhost:14268/api/traces");
            //     opts.ExportProcessorType = ExportProcessorType.Simple;
            // })
            .AddOtlpExporter(opts =>
            {
                opts.Protocol = OtlpExportProtocol.Grpc;
                opts.Endpoint = new Uri("http://localhost:4317/api/traces");
                opts.ExportProcessorType = ExportProcessorType.Simple;
            }).Build();
        var logger = new LoggerConfiguration()
            .ReadFrom
            .Configuration(config)
            .CreateLogger();
        
        using var server = new KestrelMetricServer(port: 5000);
        server.Start();
        

        // Generate some sample data from fake business logic.
        var recordsProcessed = Metrics.CreateCounter("sample_records_processed_total", "Total number of records processed.", new []{"appName"});
        logger.Information("starting consuming kafka events!");

        const string topic = "purchase";
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = config.GetValue<string>("KafkaEndpoint"),
            GroupId = "purchase",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => {
            e.Cancel = true; // prevent the process from terminating.
            cts.Cancel();
        };
        
        
        using (var consumer = new ConsumerBuilder<Null, string>(
                   consumerConfig).Build())
        {
            consumer.Subscribe(topic);
            try {
                while (true) {
                    logger.Information("[App2] started consuming event");
                    var cr = consumer.Consume(cts.Token);
                    var parentContext = Propagator.Extract(default, cr.Message.Headers, ExtractTraceContextFromMessage);
                    Baggage.Current = parentContext.Baggage;
                    using var activity = Activity.StartActivity("Process message", ActivityKind.Consumer, parentContext.ActivityContext);
                    AddExtraTags(activity);
                    logger.Information("Consumed event from topic {Topic} with key {@MessageKey} and value {MessageValue}", topic, cr.Message.Key, cr.Message.Value);
                    activity!.SetTag("message", cr.Message.Value);
                    await MakeHttpCall(cr.Message.Value, config.GetValue<string>("Api2Url"));
                    await MakeHttpCall(cr.Message.Value, $"{config.GetValue<string>("Api2Url")}/publish-message");
                    recordsProcessed.WithLabels("App2").Inc();
                    
                    
                }
            }
            catch (Exception exception) {
               logger.Error("Exception:{Message}", exception.InnerException?.Message);
            }
            finally{
                consumer.Close();
            }
        }
        logger.Information("starting consuming kafka events!");
    }


    private static void AddExtraTags(Activity? activity)
    {
        activity!.SetTag("messaging system", "Kafka");
        activity.SetTag("Topic", "purchase");
    }
    private static IEnumerable<string> ExtractTraceContextFromMessage(Headers headers, string key)
    {

            var val=headers.FirstOrDefault(x => x.Key == key)?.GetValueBytes();
            return val != null ? new[] { Encoding.UTF8.GetString(val) } : Enumerable.Empty<string>();
    }

    private static async Task MakeHttpCall(string val, string url)
    {
        var httpClient = new HttpClient();
        await httpClient.PostAsync(url,
            new StringContent(val, Encoding.UTF8,"application/json"));
    }
}