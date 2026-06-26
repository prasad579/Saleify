namespace MarketplaceCopilot.Services.Contracts;

/// <summary>
/// Resolves the user acting on the current request, so the service layer can attribute audit
/// entries without depending on ASP.NET. Implemented in the API layer over the HTTP context
/// (reads the X-Acting-User header the SPA sends), falling back to "System".
/// </summary>
public interface IActingUserAccessor
{
    string CurrentUser { get; }
}
