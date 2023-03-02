
using App4;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;

var  host = Host.CreateDefaultBuilder(args).
    ConfigureAppConfiguration(
        (hostContext, config) =>
        {
            config.SetBasePath(Directory.GetCurrentDirectory());
            config.AddJsonFile("appsettings.json", false, true);
            config.AddEnvironmentVariables();
        }
    )
    .ConfigureLogging(
        loggingBuilder =>
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            loggingBuilder.AddSerilog(logger, dispose: true);
        }
    )
    .ConfigureServices( services =>
    {
        var provider = services.BuildServiceProvider();
        var config = provider
            .GetRequiredService<IConfiguration>(); 
        
        var connection = ConnectionMultiplexer.Connect(config.GetValue<string>("ConnectionString"));
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = config.GetValue<string>("ConnectionString");
            options.InstanceName = "master";
        });
        
        services.AddOpenTelemetry().WithTracing(b =>
        {
            
            b.AddSource(nameof(Worker))
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("App4"))
                .AddRedisInstrumentation(connection)
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
                });
            
        });

        services.AddHttpClient();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();