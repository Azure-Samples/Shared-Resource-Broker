namespace Backend.Services;

using Backend.Models.Request;
using Backend.Models.Response;
using System.Threading.Tasks;

public interface IApplicationService
{
    Task<ApplicationCreatedResponse> CreateApplication(ApplicationCreateRequest req);
    Task<ApplicationDeletedResponse> DeleteApplication(string applicationId);
}
