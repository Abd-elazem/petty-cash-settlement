using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PettyCash.Application.Abstractions;

namespace PettyCash.Api.Development;

public sealed class DevelopmentAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DevelopmentCurrentUser";

    private readonly ICurrentUserContext _currentUserContext;

    public DevelopmentAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ICurrentUserContext currentUserContext)
        : base(options, logger, encoder)
    {
        _currentUserContext = currentUserContext;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        CurrentUser currentUser = _currentUserContext.Current;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, currentUser.UserId),
            new(ClaimTypes.Name, currentUser.UserId),
            new(ClaimTypes.Email, currentUser.Email)
        };

        claims.AddRange(currentUser.Roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
