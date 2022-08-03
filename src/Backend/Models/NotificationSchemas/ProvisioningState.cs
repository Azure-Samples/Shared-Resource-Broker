namespace Backend.Models.NotificationSchemas;

public enum NotificationSource
{
    MarketPlace,
    ManagedApp
}

public sealed class ProvisioningState
{
    public const string Deleting = "Deleting";
    public const string Deleted = "Deleted";
    public const string Accepted = "Accepted";
    public const string Succeeded = "Succeeded";
}
