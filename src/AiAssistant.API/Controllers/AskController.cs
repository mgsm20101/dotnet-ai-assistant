using AiAssistant.Application.Features.Ask;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AiAssistant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AskController(IMediator mediator) : ControllerBase
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

        var result = await mediator.Send(new AskCommand(request.Question), ct);
        return Ok(result);
    }
}

public sealed record AskRequest(string Question);
