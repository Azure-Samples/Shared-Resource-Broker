namespace Backend.Models.NotificationSchema;

using System;

public abstract class NotificationBase
{
    public string _eventType { get; set; }
    public string applicationId { get; set; }
    public DateTime eventTime { get; set; }
    public string provisioningState { get; set; }
}
