namespace Backend.Services;

using Azure.Identity;
using Backend.Models.Request;
using Backend.Models.Response;
using Backend.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ApplicationService : IApplicationService
{

    private readonly ILogger<ApplicationService> _logger;
    private readonly IOptions<ServicePrincipalCreatorSettings> _appSettings;

    public ApplicationService(ILogger<ApplicationService> logger, IOptions<ServicePrincipalCreatorSettings> settingsOptions)
    {
        _logger = logger;
        _appSettings = settingsOptions;
    }

    private string GetApplicationName(ApplicationCreateRequest req) =>
        $"{_appSettings.Value.GeneratedServicePrincipalPrefix}-{req.SubscriptionID}-{req.ResourceGroupName}";

    private string GetApplicationName(string resourceId)
    {
        var parts = resourceId.TrimStart('/').Split('/');

        return GetApplicationName(
            new ApplicationCreateRequest(
                SubscriptionID: Guid.Parse(parts[1]),
                ResourceGroupName: parts[3]));
    }

    private GraphServiceClient GetGraphServiceClient()
    {
        var credential = new ChainedTokenCredential(
            new ManagedIdentityCredential(_appSettings.Value.AzureADManagedIdentityClientId.ToString()),
            new EnvironmentCredential()
        );
        
        var token = credential.GetToken(
            new Azure.Core.TokenRequestContext(
                new[] { "https://graph.microsoft.com/.default" }));

        var accessToken = token.Token;
        return new(
           new DelegateAuthenticationProvider((requestMessage) =>
           {
               requestMessage.Headers.Authorization = new ("Bearer", accessToken);
               return Task.CompletedTask;
           }));
    }

    public async Task<ApplicationDeletedResponse> DeleteApplication(string applicationId)
    {
        try
        {
            var queryOptions = new List<QueryOption>
            { 
                new("$count", "true")
            };

            var app = GetApplicationName(applicationId);
            var _graphServiceClient = GetGraphServiceClient();
            var applications = await _graphServiceClient.Applications
                .Request(queryOptions)
                .Filter($"startsWith(displayName,'{app}')")
                .Header("ConsistencyLevel", "eventual")
                .GetAsync();

            _logger.LogInformation($"Deleting app: {applications.First().DisplayName}");
            await _graphServiceClient.Applications[applications.First().Id]
                .Request()
                .DeleteAsync();
            
            return new ApplicationDeletedResponse();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
            throw;
        }
    }

    public async Task<ApplicationCreatedResponse> CreateApplication(ApplicationCreateRequest req)
    {
        var key = GetApplicationName(req);
        try
        {
            GraphServiceClient _graphServiceClient = GetGraphServiceClient();
            //Search for AAD app. Make sure SP does not already exist
            var apps = await _graphServiceClient.Applications
                .Request(
                    new List<QueryOption>()
                    {
                        new("$count", "true"),
                        new("$filter", "DisplayName eq " +"'"+ key+ "'")
                    })
                .Header("ConsistencyLevel", "eventual")
                .GetAsync();

            //Ensure that only one app can be created
            if (apps.Count > 0)
            {
                throw new ArgumentException("Service principal already exist");
            }

            //Create AAD application
            var app = await _graphServiceClient
                .Applications
                .Request()
                .AddAsync(new Application { 
                    DisplayName = key, SignInAudience = "AzureADMyOrg"
                    //Notes = $"This service principal is used by customer subscription {req.SubscriptionID} / managed resource group {req.ResourceGroupName}",
                    //Description = $"/subscriptions/{req.SubscriptionID}/resourceGroups/{req.ResourceGroupName}",
                });

            _logger.LogTrace("AAD app created:" + app.DisplayName);

            //Create Secret
            var pwd = await _graphServiceClient.Applications[app.Id]
                .AddPassword(
                    new PasswordCredential
                    {
                        DisplayName = $"{_appSettings.Value.GeneratedServicePrincipalPrefix}-rbac",
                        EndDateTime = DateTime.Now.AddYears(100),
                    })
                .Request()
                .PostAsync();
            _logger.LogTrace("AAD app password  created:" + app.DisplayName);

            //Create Service principal for app
            var spr = await _graphServiceClient.ServicePrincipals
                .Request()
                .AddAsync(new ServicePrincipal { AppId = app.AppId });
            _logger.LogTrace("Service principal created:" + spr.Id);
           
            int retry = 10;
            for (int i = 0; i < retry; i++)
            {
                try
                {
                    //Add Service principal to the security group, which has permissions the resource(s).
                    await _graphServiceClient.Groups[_appSettings.Value.SharedResourcesGroup.ToString()].Members.References
                         .Request()
                         .AddAsync(new DirectoryObject { Id = spr.Id });
                    _logger.LogTrace("Service principal added to security group:" + spr.Id);
                    break;
                }
                catch (ServiceException e) when (e.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw;
                }
                catch (Exception e)
                {
                    if (i == retry)
                        _logger.LogError($"Failed to add service principal to group", e);
                    _logger.LogWarning($"Retry {i}", e);
                    Thread.Sleep(200 * i);
                }
            }

            _logger.LogDebug("Setup completed for app:" + key);
            return new ApplicationCreatedResponse(DisplayName: key, ClientSecret: pwd.SecretText, ClientId: Guid.Parse(app.AppId), TenantID: app.PublisherDomain);

        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
            throw;
        }
    }
}
