namespace Backend.Models.Response;

using System;

public record ApplicationCreatedResponse(string DisplayName, Guid ClientId, string ClientSecret, string TenantID);

public record ApplicationDeletedResponse();

