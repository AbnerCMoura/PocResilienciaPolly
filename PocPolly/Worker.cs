using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace PocPolly;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pipe = new ResiliencePipelineBuilder()
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(60)
                }).AddRetry(new RetryStrategyOptions
                {
                    Delay = TimeSpan.FromMilliseconds(1000),
                    MaxRetryAttempts = 2,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    UseJitter = true
                }).Build();  

            var result = await pipe.ExecuteAsync(async token => await FazerRequisicao(), CancellationToken.None);

            _logger.LogInformation(result.Content.ReadAsStringAsync().Result);

            await Task.Delay(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
        }
    }
    
    private async Task<HttpResponseMessage> FazerRequisicao()
    {
        try
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(7);
            return await client.GetAsync("http://viacep.com.br/ws/60335420/json/");
        }
        catch (Exception e)
        {
            var msgError = "Erro na requisição: " + e.Message;
            _logger.LogError(msgError);
            throw;
        }
    }
}