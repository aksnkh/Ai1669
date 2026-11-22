using System.Net.Http.Headers;
using System.Text.Json;
using CallCenter.Application.Abstractions;
using CallCenter.Application.DTOs;
using CallCenter.Configuration;
using Microsoft.Extensions.Options;

namespace CallCenter.Infrastructure.OpenAI;

public sealed class OpenAiSpeechClient(HttpClient client, IOptions<OpenAiOptions> settings)
    : ISpeechTranscriber, ISpeechSynthesizer
{
    private readonly OpenAiOptions options = settings.Value;

    public async Task<string> TranscribeAsync(AudioUpload audio, CancellationToken cancellationToken)
    {
        using var request = CreateRequest("audio/transcriptions");
        using var form = new MultipartFormDataContent();
        var file = new StreamContent(audio.Content);
        file.Headers.ContentType = new MediaTypeHeaderValue(audio.MediaType);
        form.Add(file, "file", audio.FileName);
        form.Add(new StringContent(options.TranscriptionModel), "model");
        form.Add(new StringContent("fa"), "language");
        form.Add(new StringContent("json"), "response_format");
        request.Content = form;

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TranscriptionResponse>(cancellationToken);
        return result?.Text ?? throw new JsonException("Missing transcription text.");
    }

    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken)
    {
        using var request = CreateRequest("audio/speech");
        request.Content = JsonContent.Create(new
        {
            model = options.SpeechModel,
            voice = options.Voice,
            input = text,
            instructions = "Speak in natural Iranian Persian with a warm, clear female voice.",
            response_format = "mp3"
        });

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var audio = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (audio.Length == 0)
            throw new HttpRequestException("Speech synthesis returned empty audio.");

        return audio;
    }

    private HttpRequestMessage CreateRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new OpenAiConfigurationException();

        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey.Trim());
        return request;
    }

    private sealed record TranscriptionResponse(string Text);
}

internal sealed class OpenAiConfigurationException : Exception;
