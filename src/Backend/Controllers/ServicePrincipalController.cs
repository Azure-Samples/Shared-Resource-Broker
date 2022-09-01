namespace Backend.Controllers;

using Backend.Models.Request;
using Backend.Models.Response;
using Backend.Models.Settings;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Linq;
using System.Threading.Tasks;

internal class UnauthorizedException : Exception { }

//[Authorize]
[ApiController]
[Route("[controller]")]
public class ServicePrincipalController : ControllerBase
{
    private readonly IOptions<ServicePrincipalCreatorSettings> _appSettings;
    private readonly IApplicationService _applicationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServicePrincipalController> _logger;

    public ServicePrincipalController(
        ILogger<ServicePrincipalController> logger, 
        IOptions<ServicePrincipalCreatorSettings> settingsOptions, 
        IApplicationService applicationService, 
        IConfiguration configuration)
    {
        _logger = logger;
        _appSettings = settingsOptions;
        _applicationService = applicationService;
        _configuration = configuration;
    }

    private async Task<SubscriptionRegistrationOkResponse> CreateServicePrincipal(SubscriptionRegistrationRequest o)
    {
        if (string.IsNullOrEmpty(o.Resourcegroup)) { throw new ArgumentNullException(paramName: nameof(o), message: $"Missing {nameof(SubscriptionRegistrationRequest)}.{nameof(SubscriptionRegistrationRequest.ResourceGroupName)}"); }
        if (string.IsNullOrEmpty(o.Subscription)) { throw new ArgumentNullException(paramName: nameof(o), message: $"Missing {nameof(SubscriptionRegistrationRequest)}.{nameof(SubscriptionRegistrationRequest.Subscription)}"); }
        
        string? secretPassed = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(secretPassed)) secretPassed = o.Secret;
        var keyVaultSecrect = _configuration["BootstrapSecret"];
        if (string.IsNullOrEmpty(secretPassed) || secretPassed != keyVaultSecrect) { throw new UnauthorizedException();  }
            

        var applicationCreatedResponse = await _applicationService.CreateApplication(
            new ApplicationCreateRequest(
                SubscriptionID: Guid.Parse(o.Subscription.Split('/').Last()),
                ResourceGroupName: o.Resourcegroup));

        return new SubscriptionRegistrationOkResponse(
            ClientId: applicationCreatedResponse.ClientId.ToString(),
            ClientSecret: applicationCreatedResponse.ClientSecret,
            TenantID: applicationCreatedResponse.TenantID);
    }

    [SwaggerResponse(StatusCodes.Status200OK, "Service principal created", typeof(SubscriptionRegistrationOkResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Service principal already exist", typeof(SubscriptionRegistrationFailedResponse))]
    [HttpPost]
    [Route("/Subscription")]
    public async Task<IActionResult> PostSubscription(SubscriptionRegistrationRequest o)
    {
        try
        {
            return Ok(await CreateServicePrincipal(o));
        }
        catch (ArgumentNullException e) { return BadRequest(new SubscriptionRegistrationFailedResponse(Message: e.Message)); }
        catch (ArgumentException e) when (e.Message.Contains("Service principal already exist")) { return Conflict(new SubscriptionRegistrationFailedResponse(Message: e.Message)); }
        catch (UnauthorizedException) { return Unauthorized(); }
        catch (Exception e) { return BadRequest(new SubscriptionRegistrationFailedResponse(Message: e.Message)); }
    }
}
