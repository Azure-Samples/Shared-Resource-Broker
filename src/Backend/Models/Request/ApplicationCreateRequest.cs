namespace Backend.Models.Request;

public record ApplicationCreateRequest(Guid SubscriptionID, string ResourceGroupName);
