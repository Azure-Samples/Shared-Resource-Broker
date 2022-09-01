namespace Backend.Models;

public record SubscriptionRegistrationRequest(string SubscriptionID, string ManagedResourceGroupName);

public record CreateServicePrincipalInKeyVaultResponse(string ClientID, string SecretURL, string TenantID); 

public record SubscriptionRegistrationOkResponse(string ClientID, string ClientSecret, string TenantID);

public record SubscriptionRegistrationFailedResponse(string Message);
