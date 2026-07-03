using IChing.Lab.Core.Rules;
using Microsoft.AspNetCore.Mvc;

namespace IChing.Lab.Api.Controllers;

[ApiController]
[Route("lab/rules/plugins")]
public sealed class RulePluginsController : ControllerBase
{
    private readonly RuleEngine _ruleEngine;

    public RulePluginsController(RuleEngine ruleEngine)
    {
        _ruleEngine = ruleEngine;
    }

    [HttpGet]
    public IActionResult List([FromQuery] string? domain = null)
    {
        var plugins = _ruleEngine.ListPlugins();
        if (!string.IsNullOrWhiteSpace(domain))
        {
            plugins = plugins.Where(p => p.Domain == domain).ToList();
        }

        return Ok(new { plugins });
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] RulePluginUpdateRequest request)
    {
        if (request.Weight is < 0 or > 1000)
        {
            return BadRequest(new { error = "weight must be between 0 and 1000" });
        }

        if (!_ruleEngine.ConfigurePlugin(id, request.Enabled, request.Weight))
        {
            return NotFound(new { error = "plugin not found" });
        }

        return Ok(_ruleEngine.ListPlugins().First(p => p.Id == id));
    }
}

public sealed record RulePluginUpdateRequest(bool? Enabled, int? Weight);
