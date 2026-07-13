using MarketplaceCopilot.Data;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(DataStore store, ITenantAccessor tenant) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<object>> GetAll(
        [FromQuery] string? marketplace,
        [FromQuery] string? family,
        [FromQuery] string? search,
        [FromQuery] bool includeDiscontinued = false)
    {
        var query = store.Products.Where(p => p.TenantId == tenant.TenantId);
        if (!includeDiscontinued)
            query = query.Where(p => !p.Discontinued);
        if (!string.IsNullOrWhiteSpace(marketplace))
            query = query.Where(p => p.Marketplaces.Any(m => m.Contains(marketplace, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(family))
            query = query.Where(p => p.Family.Contains(family, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        return Ok(query);
    }

    [HttpGet("{id}")]
    public ActionResult<object> GetById(string id)
    {
        // Tenant-scoped (including discontinued ones, unlike GetAll) so a deal that already
        // selected a since-discontinued/other-cloud product still resolves it for pricing/overview
        // display, while a cross-tenant id (probing or an otherwise-invalid state) 404s like a bad id.
        var product = store.Products.FirstOrDefault(p => p.Id == id && p.TenantId == tenant.TenantId);
        return product is null ? NotFound() : product;
    }
}
