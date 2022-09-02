namespace Backend.Middleware.Identity;

using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

public class GroupsCheckRequirement : IAuthorizationRequirement
{
    public string Group { get; init; }

    public GroupsCheckRequirement(string group) => Group = group;
}

public class GroupsCheckHandler : AuthorizationHandler<GroupsCheckRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GroupsCheckRequirement requirement)
    {
        var claims = context.User.Claims.Where(t => t.Type == "groups").ToList();

        var group = context.User.Claims
            .Where(t => t.Type == "groups" && t.Value == requirement.Group).FirstOrDefault();

        if (group is not null)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
