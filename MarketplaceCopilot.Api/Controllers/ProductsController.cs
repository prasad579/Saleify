using MarketplaceCopilot.Data;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(DataStore store) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<object>> GetAll(
        [FromQuery] string? marketplace,
        [FromQuery] string? family,
        [FromQuery] string? search)
    {
        var query = store.Products.AsEnumerable();
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
        var product = store.Products.FirstOrDefault(p => p.Id == id);
        return product is null ? NotFound() : product;
    }
}
