namespace CallCenter.Application.Abstractions;
public interface ISpeechTranscriber
{
    Task<string> TranscribeAsync(Stream audio, string fileName, CancellationToken cancellationToken);
}
