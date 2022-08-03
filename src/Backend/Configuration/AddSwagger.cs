//namespace Backend.Configuration
//{
//    public class AddSwaggerExt
//    {
//        private void AddSwagger(IServiceCollection services)
//        {
//            services.AddOpenApiDocument(document =>
//            {
//                document.AddSecurity("Bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
//                {
//                    Type = OpenApiSecuritySchemeType.OAuth2,
//                    Description = "Azure AAD Authentication",
//                    Flow = OpenApiOAuth2Flow.Implicit,
//                    Flows = new OpenApiOAuthFlows()
//                    {
//                        Implicit = new OpenApiOAuthFlow()
//                        {
//                            Scopes = new Dictionary<string, string>
//                        {
//                    { $"api://{Configuration["AzureAd:ClientId"]}/user_impersonation", "Access Application" },
//                        },
//                            AuthorizationUrl = $"{Configuration["AzureAd:Instance"]}{Configuration["AzureAd:TenantId"]}/oauth2/v2.0/authorize",
//                            TokenUrl = $"{Configuration["AzureAd:Instance"]}{Configuration["AzureAd:TenantId"]}/oauth2/v2.0/token",
//                        },
//                    },
//                });
//                document.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
//            });
//        }
//    }
//}
