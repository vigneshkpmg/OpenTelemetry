using System.Text;
using System.Text.Json;


namespace Executor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var httpClient = new HttpClient();
            var count = 1;
            while (!stoppingToken.IsCancellationRequested)
            {
                var request = new RegistrationRequest
                {
                    Id = count,
                    Name = $"vignesh-{count}",
                    Email = $"vignesh-{count}@gmail.com"
                };
                await httpClient.PostAsync(_configuration.GetValue<string>("ApiUrl"),
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8,"application/json"), stoppingToken);
                await httpClient.PostAsync($"{_configuration.GetValue<string>("ApiUrl")}/publish-message",
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8,"application/json"), stoppingToken);
                _logger.LogInformation("Worker processing record: {Count}", count);
                await Task.Delay(5000, stoppingToken);
                count++;
            }

        }
        catch (Exception e)
        {
            _logger.LogError("error : {0}",e);
        }
       
    }
}