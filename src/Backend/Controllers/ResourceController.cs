namespace Backend.Controllers;

using Backend.Models.NotificationSchemas;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using AzureMarketplaceSchema = Backend.Models.NotificationSchema.AzureMarketplaceApplication;
using ServiceCatalogSchema = Backend.Models.NotificationSchema.ServiceCatalogApplication;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/[controller]")]
[ApiController]
/*
 * https://docs.microsoft.com/en-us/azure/azure-resource-manager/managed-applications/publish-notifications#azure-marketplace-application-notification-schema
 */
public class ResourceController : ControllerBase
{
    private readonly ILogger<ResourceController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IApplicationService _applicationService;

    public ResourceController(ILogger<ResourceController> logger, IConfiguration configuration, IApplicationService applicationService)
    {
        _logger = logger;
        _configuration = configuration;
        _applicationService = applicationService;
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost]
    [Route("/resource")]
    public IActionResult ApplicationNotification(string sig)
    {
        var sec = _configuration["NotificationSecret"];
        if (sig != sec)
        {
            _logger.LogError("Marketplace attempt with wrong signature");
            return Unauthorized();
        }
        try
        {
            string json = new StreamReader(this.Request.BodyReader.AsStream()).ReadToEnd();
            dynamic clientData = JObject.Parse(json);

            NotificationSource notificationSource;

            //Determine who is calling. Azure marketplace or Managed application
            if ((clientData.billingDetails is not null) || (clientData.plan.publisher is not null))
            {
                notificationSource = NotificationSource.MarketPlace;
            }
            else if (clientData.applicationDefinitionId is not null)
            {
                notificationSource = NotificationSource.ManagedApp;
            }
            else
            {
                return BadRequest("Unrecognized payload");
            }

            string provisionState = (string)clientData.provisioningState;
            switch (provisionState, notificationSource)
            {
                case (ProvisioningState.Deleted, NotificationSource.ManagedApp):
                    var req = JsonConvert.DeserializeObject<ServiceCatalogSchema.Succeeded>(json);
                    _applicationService.DeleteApplication(req.applicationId);
                    _logger.LogDebug("Deleted");
                    break;
                case (ProvisioningState.Succeeded, NotificationSource.ManagedApp):
                    _logger.LogDebug("Succeeded");
                    break;
                case (ProvisioningState.Deleting, NotificationSource.ManagedApp):
                    _logger.LogDebug("Succeeded");
                    break;
                case (ProvisioningState.Deleted, NotificationSource.MarketPlace):
                    var request = JsonConvert.DeserializeObject<AzureMarketplaceSchema.Succeeded>(json);
                    _applicationService.DeleteApplication(request.applicationId);
                    _logger.LogDebug("Deleted");
                    break;
                case (ProvisioningState.Succeeded, NotificationSource.MarketPlace):
                    _logger.LogDebug("Succeeded");
                    break;
                case (ProvisioningState.Deleting, NotificationSource.MarketPlace):
                    _logger.LogDebug("Succeeded");
                    break;
                default:
                    _logger.LogDebug(provisionState);
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
            return BadRequest(e.Message);
        }
        return Ok();
    }
}