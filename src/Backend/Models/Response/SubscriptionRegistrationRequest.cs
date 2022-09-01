namespace Backend.Models.Response;

public record SubscriptionRegistrationOkResponse(string ClientId,  string ClientSecret, string TenantID);

public record SubscriptionRegistrationFailedResponse(string Message);

public record CreateServicePrincipalInKeyVaultResponse(string ClientId, string SecretURL, string TenantID);