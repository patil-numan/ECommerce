using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.IntegrationTests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(
                options =>
                {
                    options.DefaultAuthenticateScheme =
                        TestAuthenticationHandler.AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        TestAuthenticationHandler.AuthenticationScheme;
                })
                .AddScheme<
                    AuthenticationSchemeOptions,
                    TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationScheme,
                    _ => { });
        });
    }
}

public class TestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "Test";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        var role =
            Request.Headers["X-Test-Role"]
                .FirstOrDefault()
            ?? "Customer";

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                "integration-test-user"),

            new Claim(
                ClaimTypes.Name,
                "integration-test-user"),

            new Claim(
                ClaimTypes.Role,
                role)
        };

        var identity = new ClaimsIdentity(
            claims,
            AuthenticationScheme);

        var principal =
            new ClaimsPrincipal(identity);

        var ticket =
            new AuthenticationTicket(
                principal,
                AuthenticationScheme);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }
}