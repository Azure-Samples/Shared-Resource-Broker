namespace Backend.Models.NotificationSchema.AzureMarketplaceApplication;

public sealed class Failed : NotificationBase
{
    public Billingdetails billingDetails { get; set; }
    public Plan plan { get; set; }
    public Error error { get; set; }
}