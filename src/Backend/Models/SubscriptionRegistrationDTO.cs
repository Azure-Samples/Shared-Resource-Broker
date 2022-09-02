namespace Backend.Models;

public record SubscriptionRegistrationRequest(string SubscriptionID, string ManagedResourceGroupName);

public record CreateServicePrincipalInKeyVaultResponse(string SecretURL); 

public record SubscriptionRegistrationOkResponse(string ClientID, string ClientSecret, string TenantID);

public record SubscriptionRegistrationFailedResponse(string Message);

internal static class DTOValidation
{
    public static void EnsureValid(this SubscriptionRegistrationRequest subscriptionRegistrationRequest)
    {
        if (string.IsNullOrEmpty(subscriptionRegistrationRequest.ManagedResourceGroupName))
        {
            throw new ArgumentNullException(
                paramName: nameof(subscriptionRegistrationRequest), 
                message: $"Missing {nameof(SubscriptionRegistrationRequest)}.{nameof(SubscriptionRegistrationRequest.ManagedResourceGroupName)}");
        }
        if (string.IsNullOrEmpty(subscriptionRegistrationRequest.SubscriptionID))
        {
            throw new ArgumentNullException(
                paramName: nameof(subscriptionRegistrationRequest), 
                message: $"Missing {nameof(SubscriptionRegistrationRequest)}.{nameof(SubscriptionRegistrationRequest.SubscriptionID)}");
        }
    }
}