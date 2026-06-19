using System.Security.Claims;
using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration config, UserStore users, IWebHostEnvironment env) : ControllerBase
{
    private string FrontendUrl => config["Auth:FrontendUrl"] ?? "http://localhost:4200";
    private bool DevAutoApprove => env.IsDevelopment() && config.GetValue("Auth:AutoApproveLocalSignup", true);
    private bool DevAllowAnyLogin => env.IsDevelopment() && config.GetValue("Auth:AllowAnyLogin", true);

    [HttpPost("login")]
    public ActionResult<AuthResponse> Login([FromBody] AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new AuthResponse { Success = false, Message = "Email and password required." });

        try
        {
            var user = TryLoginLocal(request);
            return Ok(ToAuthResponse(user, "Welcome back!"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new AuthResponse { Success = false, Message = ex.Message, Email = request.Email, Status = users.FindByEmail(request.Email)?.Status });
        }
    }

    [HttpPost("signup")]
    public ActionResult<AuthResponse> Signup([FromBody] AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new AuthResponse { Success = false, Message = "All fields are required." });

        try
        {
            var user = users.RegisterLocal(request.FullName!, request.Email, request.Password, autoApprove: DevAutoApprove);
            return Ok(new AuthResponse
            {
                Success = true,
                Message = DevAutoApprove ? "Account created. You are signed in." : "Account created. Please verify your email.",
                Token = DevAutoApprove ? user.Token : null,
                Role = DevAutoApprove ? user.Role : null,
                Name = user.Name,
                Email = user.Email,
                Status = user.Status,
                Provider = "local"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new AuthResponse { Success = false, Message = ex.Message });
        }
    }

    private AppUser TryLoginLocal(AuthRequest request)
    {
        try
        {
            return users.LoginLocal(request.Email, request.Password);
        }
        catch (InvalidOperationException ex) when (DevAllowAnyLogin)
        {
            if (ex.Message.Contains("Invalid email or password", StringComparison.Ordinal))
                return users.LoginOrRegisterDev(request.Email, request.Password, request.FullName);

            var user = users.FindByEmail(request.Email);
            if (user is not null && user.Provider == "local" && user.PasswordHash == UserStore.HashPassword(request.Password))
                return users.LoginOrRegisterDev(request.Email, request.Password, request.FullName);

            throw;
        }
    }

    private static AuthResponse ToAuthResponse(AppUser user, string message) => new()
    {
        Success = true,
        Message = message,
        Token = user.Token,
        Role = user.Role,
        Name = user.Name,
        Email = user.Email,
        Status = user.Status,
        Provider = user.Provider
    };

    [HttpPost("verify-email")]
    public ActionResult<AuthResponse> VerifyEmail([FromBody] AuthRequest request)
    {
        users.MarkEmailVerified(request.Email);
        return Ok(new AuthResponse { Success = true, Message = "Email verified.", Email = request.Email, Status = "awaiting_role" });
    }

    [HttpPost("approve-role")]
    public ActionResult<AuthResponse> ApproveRole([FromBody] AuthRequest request)
    {
        users.ApproveUser(request.Email, request.FullName ?? "Sales Representative");
        var user = users.FindByEmail(request.Email)!;
        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Role assigned.",
            Token = user.Token,
            Role = user.Role,
            Name = user.Name,
            Email = user.Email,
            Status = user.Status
        });
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        if (!IsGoogleConfigured())
            return Redirect($"{FrontendUrl}/auth/callback?error=google_not_configured");

        var props = new AuthenticationProperties { RedirectUri = Url.Action(nameof(GoogleCallback)) };
        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (!result.Succeeded)
            return Redirect($"{FrontendUrl}/auth/callback?error=google_failed");

        return RedirectToFrontend(result.Principal, "google");
    }

    [HttpGet("microsoft")]
    public IActionResult MicrosoftLogin()
    {
        if (!IsMicrosoftConfigured())
            return Redirect($"{FrontendUrl}/auth/callback?error=microsoft_not_configured");

        var props = new AuthenticationProperties { RedirectUri = Url.Action(nameof(MicrosoftCallback)) };
        return Challenge(props, MicrosoftAccountDefaults.AuthenticationScheme);
    }

    [HttpGet("microsoft/callback")]
    public async Task<IActionResult> MicrosoftCallback()
    {
        var result = await HttpContext.AuthenticateAsync(MicrosoftAccountDefaults.AuthenticationScheme);
        if (!result.Succeeded)
            return Redirect($"{FrontendUrl}/auth/callback?error=microsoft_failed");

        return RedirectToFrontend(result.Principal, "microsoft");
    }

    [HttpGet("providers")]
    public ActionResult<object> Providers() => new
    {
        google = IsGoogleConfigured(),
        microsoft = IsMicrosoftConfigured(),
        googleUrl = "/api/auth/google",
        microsoftUrl = "/api/auth/microsoft"
    };

    private IActionResult RedirectToFrontend(ClaimsPrincipal principal, string provider)
    {
        var email = principal.FindFirstValue(ClaimTypes.Email)
                  ?? principal.FindFirstValue("email")
                  ?? "";
        var name = principal.FindFirstValue(ClaimTypes.Name)
                 ?? principal.FindFirstValue("name")
                 ?? email.Split('@')[0];

        if (string.IsNullOrWhiteSpace(email))
            return Redirect($"{FrontendUrl}/auth/callback?error=no_email");

        var user = users.LoginOrRegisterOAuth(email, name, provider);
        var q = $"token={Uri.EscapeDataString(user.Token)}&email={Uri.EscapeDataString(user.Email)}&name={Uri.EscapeDataString(user.Name)}&role={Uri.EscapeDataString(user.Role)}&status={Uri.EscapeDataString(user.Status)}&provider={provider}";
        return Redirect($"{FrontendUrl}/auth/callback?{q}");
    }

    private bool IsGoogleConfigured() =>
        !string.IsNullOrWhiteSpace(config["Auth:Google:ClientId"]) &&
        !string.IsNullOrWhiteSpace(config["Auth:Google:ClientSecret"]) &&
        config["Auth:Google:ClientId"] != "YOUR_GOOGLE_CLIENT_ID";

    private bool IsMicrosoftConfigured() =>
        !string.IsNullOrWhiteSpace(config["Auth:Microsoft:ClientId"]) &&
        !string.IsNullOrWhiteSpace(config["Auth:Microsoft:ClientSecret"]) &&
        config["Auth:Microsoft:ClientId"] != "YOUR_MICROSOFT_CLIENT_ID";
}
