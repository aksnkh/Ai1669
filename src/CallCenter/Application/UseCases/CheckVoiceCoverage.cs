using CallCenter.Application.Abstractions;
using CallCenter.Application.DTOs;
using CallCenter.Application.Services;
using CallCenter.Domain.Enums;

namespace CallCenter.Application.UseCases;

public sealed class CheckVoiceCoverage(
    ISpeechTranscriber transcriber,
    ICityRepository repository,
    PersianCityMatcher matcher,
    IIranianCityRecognizer recognizer,
    ISpeechSynthesizer synthesizer)
{
    public async Task<VoiceCoverageReply> ExecuteAsync(AudioUpload audio, CancellationToken cancellationToken)
    {
        var transcript = await transcriber.TranscribeAsync(audio, cancellationToken);
        return await ExecuteTextAsync(transcript, cancellationToken);
    }

    public async Task<VoiceCoverageReply> ExecuteTextAsync(string transcript, CancellationToken cancellationToken)
    {
        var (status, message) = string.IsNullOrWhiteSpace(transcript)
            ? (CoverageStatus.NoSpeech, "صداتون رو واضح نشنیدم. می‌شه دوباره بگید؟")
            : await GetReplyAsync(transcript, cancellationToken);

        return new VoiceCoverageReply(status, await synthesizer.SynthesizeAsync(message, cancellationToken));
    }

    private async Task<(CoverageStatus Status, string Message)> GetReplyAsync(
        string transcript, CancellationToken cancellationToken)
    {
        var cities = await repository.GetCitiesAsync(cancellationToken);
        var matches = matcher.Find(transcript, cities);
        if (matches.Count > 0)
        {
            return matches.Count == 1
                ? CityReply(matches[0].CityName, matches[0].IsActive)
                : MultipleCitiesReply();
        }

        var recognized = await recognizer.RecognizeAsync(transcript, cancellationToken);
        var mentionedCities = matcher.KeepMentionedNames(transcript, recognized);
        return mentionedCities.Count switch
        {
            0 => (CoverageStatus.UnknownCity, "اسم شهر رو متوجه نشدم. می‌شه دوباره بگید؟"),
            1 => CityReply(mentionedCities[0], false),
            _ => MultipleCitiesReply()
        };
    }

    private static (CoverageStatus, string) CityReply(string name, bool isActive) =>
        isActive
            ? (CoverageStatus.Active, $"آره، توی {name} خدمات داریم.")
            : (CoverageStatus.Inactive, $"نه، فعلاً توی {name} خدمات نداریم.");

    private static (CoverageStatus, string) MultipleCitiesReply() =>
        (CoverageStatus.MultipleCities, "چند تا شهر شنیدم. می‌شه فقط اسم یه شهر رو بگید؟");
}
