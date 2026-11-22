namespace CallCenter.Application.Abstractions;

public interface ISpeechSynthesizer
{
    Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken);
}
