using System.Security.Cryptography;
using System.Text;
using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Data;

public class UserStore
{
    public List<AppUser> Users { get; } = [];

    public void EnsureSeeded()
    {
        const string demoEmail = "demo@marketplace.com";
        var demo = FindByEmail(demoEmail);
        if (demo is null)
        {
            demo = new AppUser
            {
                Id = "demo001",
                Name = "Srinivas K",
                Email = demoEmail,
                Provider = "local",
                PasswordHash = HashPassword("demo123"),
                Status = "approved",
                Role = "Sales Representative"
            };
            demo.Token = CreateToken();
            Users.Add(demo);
            return;
        }

        demo.Status = "approved";
        demo.Role = string.IsNullOrWhiteSpace(demo.Role) ? "Sales Representative" : demo.Role;
        demo.PasswordHash = HashPassword("demo123");
        if (string.IsNullOrWhiteSpace(demo.Token))
            demo.Token = CreateToken();
    }

    /// <summary>Seed the Customer Portal demo account so "Try Customer Demo Login" always resolves to the Customer role.</summary>
    public void EnsureCustomerDemoSeeded()
    {
        const string customerEmail = "customer@acme.com";
        var customer = FindByEmail(customerEmail);
        if (customer is null)
        {
            customer = new AppUser
            {
                Id = "customer001",
                Name = "John Ramesh",
                Email = customerEmail,
                Company = "Acme Corporation",
                Provider = "local",
                PasswordHash = HashPassword("customer123"),
                Status = "approved",
                Role = "Customer"
            };
            customer.Token = CreateToken();
            Users.Add(customer);
            return;
        }

        customer.Status = "approved";
        customer.Role = "Customer";
        customer.Company = string.IsNullOrWhiteSpace(customer.Company) ? "Acme Corporation" : customer.Company;
        customer.PasswordHash = HashPassword("customer123");
        if (string.IsNullOrWhiteSpace(customer.Token))
            customer.Token = CreateToken();
    }

    public AppUser? FindByEmail(string email) =>
        Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    public AppUser? FindByToken(string token) =>
        Users.FirstOrDefault(u => u.Token == token);

    public string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public AppUser RegisterLocal(string fullName, string email, string password, bool autoApprove = false)
    {
        var existing = FindByEmail(email);
        if (existing is not null)
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Name = fullName,
            Email = email,
            Provider = "local",
            PasswordHash = HashPassword(password),
            Status = autoApprove ? "approved" : "pending_verification",
            Role = autoApprove ? "Sales Representative" : ""
        };
        if (autoApprove)
            user.Token = CreateToken();
        Users.Add(user);
        return user;
    }

    public AppUser LoginOrRegisterDev(string email, string password, string? fullName = null)
    {
        var user = FindByEmail(email);
        if (user is null)
        {
            var name = string.IsNullOrWhiteSpace(fullName)
                ? email.Split('@')[0]
                : fullName.Trim();
            user = RegisterLocal(name, email, password, autoApprove: true);
            return user;
        }

        if (user.Provider != "local")
            throw new InvalidOperationException($"This email uses {user.Provider} sign-in. Use that button instead.");

        if (user.PasswordHash != HashPassword(password))
            throw new InvalidOperationException("Invalid email or password.");

        if (user.Status != "approved")
        {
            user.Status = "approved";
            user.Role = string.IsNullOrEmpty(user.Role) ? "Sales Representative" : user.Role;
        }

        user.Token = CreateToken();
        return user;
    }

    public AppUser LoginLocal(string email, string password)
    {
        var user = FindByEmail(email) ?? throw new InvalidOperationException("Invalid email or password.");
        if (user.Provider != "local")
            throw new InvalidOperationException($"This email uses {user.Provider} sign-in. Use that button instead.");
        if (user.PasswordHash != HashPassword(password))
            throw new InvalidOperationException("Invalid email or password.");
        if (user.Status != "approved")
            throw new InvalidOperationException(user.Status switch
            {
                "pending_verification" => "Please verify your email first.",
                "awaiting_role" => "Your account is awaiting admin role assignment.",
                _ => "Account not approved yet."
            });
        user.Token = CreateToken();
        return user;
    }

    public AppUser LoginOrRegisterOAuth(string email, string name, string provider)
    {
        var user = FindByEmail(email);
        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid().ToString("N")[..12],
                Email = email,
                Name = name,
                Provider = provider,
                Status = "approved",
                Role = "Sales Representative"
            };
            Users.Add(user);
        }
        else if (user.Provider != provider && user.Provider != "local")
        {
            user.Provider = provider;
        }
        else if (user.Provider == "local" && user.Status != "approved")
        {
            user.Status = "approved";
            user.Role = string.IsNullOrEmpty(user.Role) ? "Sales Representative" : user.Role;
        }
        else if (string.IsNullOrEmpty(user.Role))
        {
            user.Role = "Sales Representative";
            user.Status = "approved";
        }

        user.Name = string.IsNullOrWhiteSpace(user.Name) ? name : user.Name;
        user.Token = CreateToken();
        return user;
    }

    public void ApproveUser(string email, string role = "Sales Representative")
    {
        var user = FindByEmail(email) ?? throw new InvalidOperationException("User not found.");
        user.Status = "approved";
        user.Role = role;
        user.Token = CreateToken();
    }

    public void MarkEmailVerified(string email)
    {
        var user = FindByEmail(email);
        if (user is null) return;
        user.Status = user.Status == "pending_verification" ? "awaiting_role" : user.Status;
    }
}
