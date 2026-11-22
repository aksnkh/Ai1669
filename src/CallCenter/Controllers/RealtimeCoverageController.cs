using System.ComponentModel.DataAnnotations;
using CallCenter.Application.UseCases;
using CallCenter.Infrastructure.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CallCenter.Controllers;

public sealed record RealtimeSessionRequest([Required, RegularExpression("^fa$")] string Language);
public sealed record VoiceCoverageTextRequest([Required, MaxLength(2000)] string Transcript);

[ApiController]
[Route("api/voice-coverage")]
[EnableRateLimiting("realtime")]
[Consumes("application/json")]
[RequestSizeLimit(16 * 1024)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RealtimeCoverageController(
    OpenAiSpeechClient speechClient,
    CheckVoiceCoverage useCase,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    private bool Enabled => configuration.GetValue("Realtime:Enabled", environment.IsDevelopment());

    [HttpGet("realtime/model")]
    public IActionResult GetModel()
    {
        if (!Enabled)
            return NotFound();

        return Ok(new { model = speechClient.RealtimeTranscriptionModel });
    }

    [HttpPost("realtime/session")]
    public async Task<ActionResult<RealtimeClientSecret>> CreateSession(
        [FromBody] RealtimeSessionRequest request, CancellationToken cancellationToken)
    {
        if (!Enabled)
            return NotFound();

        return Ok(await speechClient.CreateRealtimeSessionAsync(cancellationToken));
    }

    [HttpPost("text")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "audio/mpeg")]
    public async Task<IActionResult> CheckText(
        [FromBody] VoiceCoverageTextRequest request, CancellationToken cancellationToken)
    {
        if (!Enabled)
            return NotFound();

        var reply = await useCase.ExecuteTextAsync(request.Transcript.Trim(), cancellationToken);
        Response.Headers["X-Audio-Generated-By"] = "AI";
        Response.Headers["X-Coverage-Status"] = reply.Status.ToString();
        return File(reply.Audio, "audio/mpeg", "voice-coverage.mp3");
    }
}
