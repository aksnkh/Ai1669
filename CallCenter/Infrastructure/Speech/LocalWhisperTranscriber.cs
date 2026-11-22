using System.Text.Json.Serialization;
using CallCenter.Application.Abstractions;

namespace CallCenter.Infrastructure.Speech;

public sealed class LocalWhisperTranscriber(HttpClient client) : ISpeechTranscriber
{
    public async Task<string> TranscribeAsync(Stream audio, string fileName, CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StreamContent(audio), "audio", Path.GetFileName(fileName));

        using var response = await client.PostAsync("transcribe", form, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Transcription>(cancellationToken);
        return result?.Text ?? throw new HttpRequestException("Local Whisper returned an invalid response.");
    }

    private sealed record Transcription([property: JsonPropertyName("text")] string Text);
}
