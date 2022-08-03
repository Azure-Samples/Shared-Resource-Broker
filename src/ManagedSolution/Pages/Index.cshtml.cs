using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ManagedSolution.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ManagedSolution.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IOptions<AppSettings> _appsettings;
        SecretClientOptions options = new SecretClientOptions()
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
            string secretValue;
            try
            {
                KeyVaultSecret secret = client.GetSecret("clientCredentials");
                 secretValue = secret.Value;
            }
            catch (Exception)
            {
                throw;
            }           
            if (string.IsNullOrEmpty(secretValue))
                throw new Exception("Secret not found");

            await Task.FromResult("Ok");
        }
        public void OnGet()
        {

        }
    }
}