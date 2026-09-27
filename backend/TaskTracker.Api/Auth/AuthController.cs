using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TaskTracker.Api.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IConfiguration configuration, IAntiforgery antiforgery) : ControllerBase
{
    private string FrontendOrigin => (configuration["Frontend:Origin"] ?? "http://localhost:4200").TrimEnd('/');

    // GET /api/auth/login: start browser sign-in through Keycloak.
    [HttpGet("login")]
    [EndpointSummary("Sign in with Keycloak")]
    [EndpointDescription("Redirects the browser to Keycloak using OpenID Connect. Optional returnUrl must be an absolute URL on the configured frontend origin; defaults to its /tasks page. An invalid returnUrl returns 400. After successful sign-in, the API creates a session cookie and redirects to returnUrl. Use browser navigation rather than an AJAX request.")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        var destination = returnUrl ?? FrontendOrigin + "/tasks";
        // Only allow redirects back to our frontend after sign-in.
        if (!Uri.TryCreate(destination, UriKind.Absolute, out var uri) ||
            uri.GetLeftPart(UriPartial.Authority) != FrontendOrigin || !string.IsNullOrEmpty(uri.UserInfo))
            return Problem(statusCode: 400, title: "Invalid return URL.");
        return Challenge(new AuthenticationProperties { RedirectUri = destination },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Authorize]
    // End both the local cookie session and the Keycloak session.
    [HttpPost("logout")]
    [EndpointSummary("Sign out of the application and Keycloak")]
    [EndpointDescription("Requires a signed-in session and an antiforgery token, supplied in the X-XSRF-TOKEN header or __RequestVerificationToken form field. Clears the local session cookie and redirects the browser to Keycloak for logout. The logout callback then redirects to the frontend /tasks page. Returns 401 when signed out and 400 for an invalid or missing antiforgery token. Use a browser POST form to follow the redirects.")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout() => SignOut(
        new AuthenticationProperties { RedirectUri = FrontendOrigin + "/tasks" },
        CookieAuthenticationDefaults.AuthenticationScheme,
        OpenIdConnectDefaults.AuthenticationScheme);

    // GET /api/auth/me: check the current session and get a token for write requests.
    [HttpGet("me")]
    [EndpointSummary("Get the current session")]
    [EndpointDescription("Available with or without sign-in. Returns authenticated, name, and csrfToken. Signed-out users receive authenticated=false and name=null. Sets the JavaScript-readable XSRF-TOKEN cookie used by Angular and the antiforgery cookie used by the server. Retain cookies and send the token on subsequent write requests. This response must not be cached.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Me()
    {
        // Angular reads this token and sends it with write requests to prevent cross-site request forgery.
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
            new CookieOptions { HttpOnly = false, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax, Path = "/" });
        return Ok(new
        {
            authenticated = User.Identity?.IsAuthenticated == true,
            name = User.Identity?.Name,
            csrfToken = tokens.RequestToken
        });
    }
}
