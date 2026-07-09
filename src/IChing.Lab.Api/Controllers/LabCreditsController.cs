using IChing.Lab.Api.Contracts;
using IChing.Lab.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IChing.Lab.Api.Controllers;

public sealed partial class LabController
{
    [HttpPost("credits/consume")]
    public async Task<IActionResult> ConsumeCredits(
        [FromBody] LabCreditsConsumeRequest req,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(req.ExchangeId))
        {
            return BadRequest(new { error = "exchangeId is required" });
        }

        var auth = Request.Headers.Authorization.ToString();
        var tier = req.Tier <= 0 ? 1 : req.Tier;
        var result = await _reads.ConsumeCreditsAsync(auth, tier, req.ExchangeId, cancellationToken);
        if (result.Allowed)
        {
            return Ok(new { ok = true, skipped = result.SkippedBecauseDisabled });
        }

        var status = result.Error?.Contains("Token", StringComparison.OrdinalIgnoreCase) == true ? 401 : 402;
        return new ObjectResult(new { error = result.Error }) { StatusCode = status };
    }
}
