using AiAssistant.Application.Features.Ask;
using Microsoft.AspNetCore.Mvc;

namespace AiAssistant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AskController(AskHandler handler) : ControllerBase
{
    /// <summary>Ask a question — routed to RAG or LLM based on intent.</summary>
    [HttpPost]
    [ProducesResponseType<AskResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ask(
        [FromBody] AskRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "Question is required." });

        var result = await handler.Handle(request, ct);
        return Ok(result);
    }
}
