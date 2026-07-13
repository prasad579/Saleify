using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

/// <summary>The current caller's tenant and its marketplace connections (connect / sync / disconnect a cloud).</summary>
[ApiController]
[Route("api/tenants")]
public class TenantsController(ITenantService tenants, ITenantAccessor tenant) : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<object> Me()
    {
        var t = tenants.GetById(tenant.TenantId);
        return t is null ? NotFound() : t;
    }

    [HttpPost("me/connections/{cloud}/connect")]
    public async Task<ActionResult<object>> Connect(string cloud, [FromBody] ConnectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SellerLabel))
            return BadRequest(new { success = false, message = "A seller/account label is required." });

        try
        {
            var t = await tenants.ConnectAsync(tenant.TenantId, cloud, request.SellerLabel.Trim());
            return Ok(new { success = true, tenant = t });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("me/connections/{cloud}/sync")]
    public async Task<ActionResult<object>> Sync(string cloud)
    {
        try
        {
            var t = await tenants.SyncAsync(tenant.TenantId, cloud);
            return Ok(new { success = true, tenant = t });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("me/connections/{cloud}/disconnect")]
    public ActionResult<object> Disconnect(string cloud)
    {
        try
        {
            var t = tenants.Disconnect(tenant.TenantId, cloud);
            return Ok(new { success = true, tenant = t });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public class ConnectRequest
{
    public string SellerLabel { get; set; } = "";
}
