using CallCenter.Application.DTOs;
using CallCenter.Application.UseCases;
using CallCenter.Infrastructure.Audio;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Controllers;

public sealed class VoiceCoverageForm
{
    public IFormFile? Audio { get; init; }
}

[ApiController]
[Route("api/voice-coverage")]
public sealed class VoiceCoverageController(CheckVoiceCoverage useCase) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "audio/mpeg")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    [RequestSizeLimit(AudioFormats.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = AudioFormats.MaxRequestBytes)]
    public async Task<IActionResult> Post([FromForm] VoiceCoverageForm form, CancellationToken cancellationToken)
    {
        if (form.Audio is not { Length: > 0 } file)
            return Problem(statusCode: 400, title: "فایل صوتی لازم است.");

        if (file.Length > AudioFormats.MaxAudioBytes)
            return Problem(statusCode: 413, title: "حجم فایل صوتی بیشتر از ۲۴ مگابایت است.");

        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        var mediaType = AudioFormats.MediaTypeFor(fileName);
        if (mediaType is null)
            return Problem(statusCode: 415, title: "فرمت فایل صوتی پشتیبانی نمی‌شود.");

        await using var stream = file.OpenReadStream();
        var reply = await useCase.ExecuteAsync(new AudioUpload(stream, fileName, mediaType), cancellationToken);
        Response.Headers["X-Audio-Generated-By"] = "AI";
        Response.Headers["X-Coverage-Status"] = reply.Status.ToString();
        Response.Headers.CacheControl = "no-store";
        return File(reply.Audio, "audio/mpeg", "voice-coverage.mp3");
    }
}
