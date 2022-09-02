using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ManagedSolution.Models.Settings;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace ManagedSolution.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IOptions<AppSettings> _appsettings;
        
        readonly SecretClientOptions options = new ()
        {
            Retry =
            {
                Delay= TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromSeconds(16),
                MaxRetries = 5,
                Mode = RetryMode.Exponential
             }
        };

        public string? Data { get; set; }
        
        public IndexModel(ILogger<IndexModel> logger, IOptions<AppSettings> appsettings)
        {
            _logger = logger;
            _appsettings = appsettings;
        }

        public Task OnPostPayloadAsync(CancellationToken ct, string data) => PushPayload(ct, data);

        private async Task PushPayload(CancellationToken ct, string payload)
        {
            var client = new SecretClient(new Uri(_appsettings.Value.KeyVaultUri), new DefaultAzureCredential(), options);

            KeyVaultSecret secret = await client.GetSecretAsync("clientCredentials", cancellationToken: ct);
            string secretValue = secret.Value;

            if (string.IsNullOrEmpty(secretValue))
            {
                throw new Exception("Secret not found");
            }
        }

        public void OnGet() { }
    }
}