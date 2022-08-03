namespace Backend.Models.NotificationSchema.ServiceCatalogApplication;

public sealed class Failed : NotificationBase
{
    public string applicationDefinitionId { get; set; }
    public Error error { get; set; }
}
