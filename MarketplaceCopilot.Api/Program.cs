using System.Text.Json;
using System.Text.Json.Serialization;
using MarketplaceCopilot.Api;
using MarketplaceCopilot.Data;
using MarketplaceCopilot.Services;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;

var builder = WebApplication.CreateBuilder(args);

// Data layer (in-memory / JSON-backed stores)
builder.Services.AddSingleton<DataStore>();
builder.Services.AddSingleton<UserStore>();

// Resolve the acting user from the current request (X-Acting-User header) for audit attribution.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IActingUserAccessor, HttpActingUserAccessor>();

// Service layer (registered against their contracts)
builder.Services.AddSingleton<IAuditService, AuditService>();
builder.Services.AddSingleton<IAiService, AiService>();
builder.Services.AddSingleton<IPricingService, PricingService>();
builder.Services.AddSingleton<IDealHistoryService, DealHistoryService>();
builder.Services.AddSingleton<IMeetingNotesService, MeetingNotesService>();
builder.Services.AddSingleton<IApprovalDocumentService, ApprovalDocumentService>();
builder.Services.AddSingleton<IApprovalService, ApprovalService>();
builder.Services.AddSingleton<IDealService, DealService>();
builder.Services.AddSingleton<IOfferRequestService, OfferRequestService>();
builder.Services.AddSingleton<ISnapshotService, SnapshotService>();
builder.Services.AddSingleton<IEngagementRequestService, EngagementRequestService>();

var authSection = builder.Configuration.GetSection("Auth");
var frontendUrl = authSection["FrontendUrl"] ?? "http://localhost:4200";

var googleClientId = authSection["Google:ClientId"];
var googleSecret = authSection["Google:ClientSecret"];
var msClientId = authSection["Microsoft:ClientId"];
var msSecret = authSection["Microsoft:ClientSecret"];

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

if (!string.IsNullOrWhiteSpace(googleClientId) && googleClientId != "YOUR_GOOGLE_CLIENT_ID" && !string.IsNullOrWhiteSpace(googleSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleSecret;
        options.CallbackPath = "/api/auth/google/callback";
        options.Scope.Add("email");
        options.Scope.Add("profile");
    });
}

if (!string.IsNullOrWhiteSpace(msClientId) && msClientId != "YOUR_MICROSOFT_CLIENT_ID" && !string.IsNullOrWhiteSpace(msSecret))
{
    authBuilder.AddMicrosoftAccount(options =>
    {
        options.ClientId = msClientId;
        options.ClientSecret = msSecret;
        options.CallbackPath = "/api/auth/microsoft/callback";
    });
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(frontendUrl, "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

app.Services.GetRequiredService<UserStore>().EnsureSeeded();
app.Services.GetRequiredService<UserStore>().EnsureCustomerDemoSeeded();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
