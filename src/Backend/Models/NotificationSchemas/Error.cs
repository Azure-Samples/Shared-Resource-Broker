namespace Backend.Models.NotificationSchema;

public sealed class Error
{
    public string code { get; set; }
    public string message { get; set; }
    public Detail[] details { get; set; }
}