using CallCenter.Application.Features.VoiceCoverage;
using Microsoft.AspNetCore.Mvc;
namespace CallCenter.Controllers;
public sealed class VoiceUploadRequest
{
    public required IFormFile Audio { get; init; }
}
[ApiController]
[Route("api/voice-coverage")]
public sealed class VoiceCoverageController(VoiceCoverageService service) : ControllerBase
{
    private const long MaxBytes = 20 * 1024 * 1024;
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        { ".wav", ".mp3", ".m4a", ".mp4", ".mpeg", ".mpga", ".webm" };

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxBytes + 1024 * 1024)]
    [ProducesResponseType<VoiceCoverageResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<VoiceCoverageResult>> Check([FromForm] VoiceUploadRequest request, CancellationToken cancellationToken)
    {
        if (request.Audio is null || request.Audio.Length == 0)
            return Problem(statusCode: 400, title: "فایل صوتی خالی است.");
        if (request.Audio.Length > MaxBytes)
            return Problem(statusCode: 413, title: "حداکثر حجم فایل صوتی ۲۰ مگابایت است.");
        if (!Extensions.Contains(Path.GetExtension(request.Audio.FileName)))
            return Problem(statusCode: 400, title: "فرمت صوت پشتیبانی نمی‌شود.");
        await using var audio = request.Audio.OpenReadStream();
        return Ok(await service.CheckAsync(audio, request.Audio.FileName, cancellationToken));
    }
}
