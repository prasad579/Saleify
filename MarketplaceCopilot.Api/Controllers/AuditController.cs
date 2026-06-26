using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

/// <summary>
/// Read-only access to the application-wide audit log (who changed what, when, with details).
/// Supports filtering by category, by affected entity, and free-text search, with pagination.
/// </summary>
[ApiController]
[Route("api/audit")]
public class AuditController(IAuditService audit) : ControllerBase
{
    [HttpGet]
    public ActionResult<AuditLogPage> Get(
        [FromQuery] string? category = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
        => audit.Query(category, entityId, search, page, pageSize);
}
