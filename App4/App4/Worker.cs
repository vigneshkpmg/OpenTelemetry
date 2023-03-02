using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Caching.Distributed;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Prometheus;

namespace App4;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private readonly IConfiguration _configuration;
    private static readonly ActivitySource Activity = new(nameof(Worker));
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    private readonly IDistributedCache _distributedCache;
    private readonly IMetricServer _metricServer;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, IDistributedCache distributedCache)
    {
        _logger = logger;
        _configuration = configuration;
        _distributedCache = distributedCache;
        _metricServer = new MetricServer(port: 3500);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        try
        {
            _metricServer.Start();
        }
        catch (HttpListenerException ex)
        {
           _logger.LogError("error while starting metrics server", ex);
        }
        
        // Generate some sample data from fake business logic.
        // Generate some sample data from fake business logic.
  
        var recordsProcessed = Metrics.CreateCounter("sample_records_processed_total", "Total number of records processed.", new []{"appName"});
        const string topic = "purchase2";
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _configuration.GetValue<string>("KafkaEndpoint"),
            GroupId = "purchase2",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        using var consumer = new ConsumerBuilder<Null, string>(
            consumerConfig).Build();
        consumer.Subscribe(topic);
        try
        {
            
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("[App4] started consuming event.");
                var cr = consumer.Consume();
                var parentContext = Propagator.Extract(default, cr.Message.Headers, ExtractTraceContextFromMessage);
                Baggage.Current = parentContext.Baggage;
                using var activity = Activity.StartActivity("Process message", ActivityKind.Consumer,
                    parentContext.ActivityContext);
                AddExtraTags(activity);
                _logger.LogInformation(
                    "Consumed event from topic {Topic} with key {@MessageKey} and value {MessageValue}", topic,
                    cr.Message.Key, cr.Message.Value);
                activity!.SetTag("message", cr.Message.Value);
                using var activity1 = Activity.StartActivity("Redis cache saving", ActivityKind.Server);
                activity1?.SetTag("key", "purchase");
                await _distributedCache.SetStringAsync("purchase", JsonSerializer.Serialize(cr.Message.Value), token: stoppingToken);
                recordsProcessed.WithLabels("App4").Inc();
            }
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogError("Exception:{Message}", exception.InnerException?.Message);
        }
        finally
        {
            consumer.Close();
        }
        _logger.LogInformation("starting consuming kafka events!");
    }
    
    private static void AddExtraTags(Activity? activity)
    {
        activity!.SetTag("messaging system", "Kafka");
        activity.SetTag("Topic", "purchase2");
    }
    private static IEnumerable<string> ExtractTraceContextFromMessage(Headers headers, string key)
    {

        var val=headers.FirstOrDefault(x => x.Key == key)?.GetValueBytes();
        return val != null ? new[] { System.Text.Encoding.UTF8.GetString(val)} : Enumerable.Empty<string>();
    }
}