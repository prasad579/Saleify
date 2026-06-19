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
