namespace Backend.Models.NotificationSchema.AzureMarketplaceApplication;

public sealed class Succeeded : NotificationBase
{
    public Billingdetails billingDetails { get; set; }
    
    public Plan plan { get; set; }
}
