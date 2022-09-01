namespace Backend.Models.Request;

public record SubscriptionRegistrationRequest(string SubscriptionID, string ResourceGroupName)
{
    public string Secret { get; init; }
    public string Resourcegroup { get; init; }
    public string Subscription { get; init; }
}