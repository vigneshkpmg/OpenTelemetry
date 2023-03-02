using App3;
using App3.Controllers;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Exporter;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<UserContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UserContext")));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Host.UseSerilog((context, provider, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});
builder.Services.AddOpenTelemetry().WithTracing(b =>
{
    b.AddAspNetCoreInstrumentation()
        .AddSource(nameof(PublishMessageController))
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("App3"))
        .AddAspNetCoreInstrumentation()
        .AddSqlClientInstrumentation()
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
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}

app.UseRouting();
app.UseHttpMetrics(options =>
{
    options.AddCustomLabel("appName", _ => "App3");
});
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<UserContext>();
    context.Database.EnsureCreated();
    // DbInitializer.Initialize(context);
}

app.UseCors("CorsPolicy");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseMetricServer();
app.Run();