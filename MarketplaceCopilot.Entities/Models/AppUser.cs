namespace MarketplaceCopilot.Entities;

public class AppUser
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "local";
    public string Role { get; set; } = "";
    public string Status { get; set; } = "pending_verification";
    public string? PasswordHash { get; set; }
    public string Token { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Organization name — shown in the Customer Portal header, set for Customer-role users.</summary>
    public string Company { get; set; } = "";
    /// <summary>The tenant this user belongs to — scopes which deals/products they can see.</summary>
    public string TenantId { get; set; } = "";
}

public class OAuthCallbackResult
{
    public string Token { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "";
    public string Role { get; set; } = "";
    public string Status { get; set; } = "";
}
