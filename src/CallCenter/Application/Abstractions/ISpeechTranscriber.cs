using CallCenter.Application.DTOs;

namespace CallCenter.Application.Abstractions;

public interface ISpeechTranscriber
{
    Task<string> TranscribeAsync(AudioUpload audio, CancellationToken cancellationToken);
}
