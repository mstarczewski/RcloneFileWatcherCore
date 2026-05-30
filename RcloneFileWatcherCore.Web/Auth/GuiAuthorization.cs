using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.Web.Auth
{
    /// <summary>
    /// Authorization that is evaluated per request against the current <see cref="IAuthService"/>
    /// state, so toggling the password requirement in the GUI takes effect immediately without a
    /// restart: when auth is disabled everyone is allowed; when enabled an authenticated user is
    /// required (unauthenticated requests are challenged → redirected to the login page).
    /// </summary>
    public sealed class GuiAuthRequirement : IAuthorizationRequirement
    {
    }

    public sealed class GuiAuthHandler : AuthorizationHandler<GuiAuthRequirement>
    {
        private readonly IAuthService _auth;

        public GuiAuthHandler(IAuthService auth)
        {
            _auth = auth;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GuiAuthRequirement requirement)
        {
            if (!_auth.Enabled || context.User?.Identity?.IsAuthenticated == true)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
