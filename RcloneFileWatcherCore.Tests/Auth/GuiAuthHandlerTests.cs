using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RcloneFileWatcherCore.Web.Auth;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.Tests.Auth
{
    [TestClass]
    public class GuiAuthHandlerTests
    {
        private static async Task<bool> Evaluate(bool enabled, ClaimsPrincipal user)
        {
            var handler = new GuiAuthHandler(new StubAuth { Enabled = enabled });
            var requirement = new GuiAuthRequirement();
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource: null);
            await handler.HandleAsync(context);
            return context.HasSucceeded;
        }

        private static ClaimsPrincipal Anonymous() => new ClaimsPrincipal(new ClaimsIdentity());

        // An identity with an authentication type is treated as authenticated.
        private static ClaimsPrincipal Authenticated() => new ClaimsPrincipal(new ClaimsIdentity("cookie"));

        [TestMethod]
        public async Task Disabled_AllowsAnonymous()
            => Assert.IsTrue(await Evaluate(enabled: false, Anonymous()));

        [TestMethod]
        public async Task Enabled_DeniesAnonymous()
            => Assert.IsFalse(await Evaluate(enabled: true, Anonymous()));

        [TestMethod]
        public async Task Enabled_AllowsAuthenticated()
            => Assert.IsTrue(await Evaluate(enabled: true, Authenticated()));

        private sealed class StubAuth : IAuthService
        {
            public bool Enabled { get; set; }
            public bool HasPassword => false;
            public bool Verify(string password) => false;
            public void SetPassword(string password) { }
            public void SetEnabled(bool enabled) => Enabled = enabled;
        }
    }
}
