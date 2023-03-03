using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace App3.Controllers;

[ApiController]
[Route("[controller]")]
public class MessageController : Controller
{
    private readonly UserContext _userContext;
    private readonly ILogger<MessageController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private const string Topic = "purchase2";
    private static readonly ActivitySource ActivitySource = new(nameof(MessageController));
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    public MessageController(ILogger<MessageController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration, UserContext userContext)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _userContext = userContext;
    }
    // post
    [HttpPost("publish-message")]
    public async Task<IActionResult> Publish(RegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("[App3][publish-message] started");
            using var activity = ActivitySource.StartActivity("Kafka Publish", ActivityKind.Producer);
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _configuration.GetValue<string>("KafkaEndpoint"),
                Acks = Acks.Leader,
                AllowAutoCreateTopics = true
            };
            var header = new Headers();

            AddActivityToHeader(activity, header);
            using var producer = new ProducerBuilder<Null,string>(producerConfig).Build();
            
            var result = await producer.ProduceAsync(Topic,
                new Message<Null, string> { Value = JsonSerializer.Serialize(request), Headers = header});

            _logger.LogInformation("{Message}  was produced to Partition {Partition} and Topic {Topic} ",
                result.Message, result.Partition, Topic);
            _logger.LogInformation("[App3][publish-message] Ended");
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
            _logger.LogInformation("[App3][default-Post] started");
            var newRequest = new RegistrationRequest
            {
                Name = request.Name,
                Email = request.Email
            };
            await _userContext.Registration.AddAsync(newRequest);
            await _userContext.SaveChangesAsync();
            _logger.LogInformation("[App3][default-Post] Ended");
            return Created("", "success");
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
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination_kind", "topic");
        activity?.SetTag("messaging.kafka.topic", "purchase");
    }

    private void InjectContextIntoHeader(Headers headers, string key, string value)
    {
        try
        {
            headers.Add( key, Encoding.UTF8.GetBytes(value));

        }
        catch (Exception e)
        {
            _logger.LogError("Error: {Exception}", e);
        }
    }
}