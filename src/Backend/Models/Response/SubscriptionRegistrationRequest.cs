namespace Backend.Models.Response;

using System;

public record SubscriptionRegistrationOkResponse(string ClientId,  string ClientSecret, string TenantID);

public record SubscriptionRegistrationFailedResponse(string Message);

