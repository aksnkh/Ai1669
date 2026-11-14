using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CallCenter.Application.Abstractions;
using Microsoft.Extensions.Options;
namespace CallCenter.Infrastructure.Speech;
public sealed class OpenAiSpeechTranscriber(HttpClient client, IOptions<SpeechOptions> options) : ISpeechTranscriber
{
    public async Task<string> TranscribeAsync(Stream audio, string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
            throw new InvalidOperationException("Speech:ApiKey is not configured.");
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(options.Value.Model), "model");
        form.Add(new StringContent("fa"), "language");
        form.Add(new StreamContent(audio), "file", Path.GetFileName(fileName));
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions")
        { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("Speech transcription provider request failed.", null, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Transcription>(cancellationToken);
        return result?.Text ?? throw new HttpRequestException("Speech provider returned an invalid response.");
    }
    private sealed record Transcription([property: JsonPropertyName("text")] string Text);
}
