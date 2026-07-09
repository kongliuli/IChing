using IChing.Lab.Api.Contracts;
using IChing.Lab.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IChing.Lab.Api.Controllers;

public sealed partial class LabController
{
    [HttpPost("chat")]
    public Task<IActionResult> Chat([FromBody] LabChatRequest req, CancellationToken cancellationToken = default) =>
        _chat.ExecuteAsync(req, cancellationToken);
}
