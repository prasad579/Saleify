using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Api;

/// <summary>
/// Resolves the acting user for audit attribution from the current HTTP request. The SPA sends the
/// signed-in user's name in the <c>X-Acting-User</c> header; when absent we fall back to "System".
/// </summary>
public class HttpActingUserAccessor(IHttpContextAccessor http) : IActingUserAccessor
{
    public string CurrentUser
    {
        get
        {
            var header = http.HttpContext?.Request.Headers["X-Acting-User"].ToString();
            return string.IsNullOrWhiteSpace(header) ? "System" : header.Trim();
        }
    }
}
