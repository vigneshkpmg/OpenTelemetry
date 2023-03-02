using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Microsoft.Extensions.Configuration;

namespace API1.Controllers;

[ApiController]
[Route("[controller]")]
public class PublishMessageController : Controller
{
    private readonly ILogger<PublishMessageController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private const string Topic = "purchase";
    private static readonly ActivitySource ActivitySource = new(nameof(PublishMessageController));
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    public PublishMessageController(ILogger<PublishMessageController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }
    // post
    [HttpPost("publish-message")]
    public async Task<IActionResult> Publish(RegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("[APP1][publish-message] started");
            using var activity = ActivitySource.StartActivity("KafkaPublish", ActivityKind.Producer);
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _configuration.GetValue<string>("KafkaEndpoint"),
                Acks = Acks.Leader,
                AllowAutoCreateTopics = true
            };
            var header = new Headers();

            if (activity != null) 
                AddActivityToHeader(activity, header);
            
            using var producer = new ProducerBuilder<Null,string>(producerConfig).Build();
            var val = JsonSerializer.Serialize(request);
            var result = await producer.ProduceAsync(Topic,
                new Message<Null, string> { Value = val, Headers = header});

            _logger.LogInformation("{ResultMessage}  was produced to Partition {Partition} and Topic {Topic} ",
                result.Message.Value, result.Partition, Topic);
            _logger.LogInformation("[APP1][publish-message] Ended");
            return Created("", "Success");
        }
        catch (Exception exception)
        {
            _logger.LogError("Error while publishing to Kafka and error is {Exception}", exception);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
       
    }
    
    [HttpPost]
    public async Task<IActionResult> HttpCall([FromBody]RegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("[APP1][Default-Post] started");
            var httpClient = _httpClientFactory.CreateClient("test");
            var record = JsonSerializer.Serialize(request);
            var content = new StringContent(record, Encoding.UTF8,"application/json");
            var result=await httpClient.PostAsync(_configuration.GetValue<string>("Api2Url"), content);
            if (result.IsSuccessStatusCode)
            {
                return Created("", "success");
            }
            _logger.LogInformation("[APP1][Default-Post] Ended");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception exception)
        {
            _logger.LogError("Error while publishing to Kafka and error is {Exception}", exception);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

    }
    
    private void AddActivityToHeader(Activity activity, Headers props)
    {
        Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), props, InjectContextIntoHeader);
        activity.SetTag("messaging.system", "kafka");
        activity.SetTag("messaging.destination_kind", "topic");
        activity.SetTag("messaging.kafka.topic", Topic);
    }

    private void InjectContextIntoHeader(Headers headers, string key, string value)
    {
        try
        {
            headers.Add( key, Encoding.UTF8.GetBytes(value));

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}