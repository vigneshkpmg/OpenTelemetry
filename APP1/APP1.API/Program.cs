using API1.Controllers;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Host
    .ConfigureLogging((_, loggingBuilder) => loggingBuilder.ClearProviders())
    .UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));
builder.Services.AddHttpClient("test").UseHttpClientMetrics();

builder.Services.AddOpenTelemetry().WithTracing(b =>
{
    b.AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource(nameof(MessageController))
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("App1"))
        .AddOtlpExporter(opts =>
        {
            opts.Protocol = OtlpExportProtocol.Grpc;
            opts.Endpoint = new Uri("http://localhost:4317/api/traces");
            opts.ExportProcessorType = ExportProcessorType.Simple;
        });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        policy => policy
            .SetIsOriginAllowed((host) => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpMetrics(options =>
{
    options.AddCustomLabel("appName", _ => "App1");
});
app.UseCors("CorsPolicy");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseMetricServer();
app.Run();