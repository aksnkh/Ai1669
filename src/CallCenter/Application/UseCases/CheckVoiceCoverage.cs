using CallCenter.Application.Abstractions;
using CallCenter.Application.DTOs;
using CallCenter.Application.Services;
using CallCenter.Domain.Entities;
using CallCenter.Domain.Enums;

namespace CallCenter.Application.UseCases;

public sealed class CheckVoiceCoverage(
    ISpeechTranscriber transcriber,
    ICityRepository repository,
    PersianCityMatcher matcher,
    ISpeechSynthesizer synthesizer)
{
    public async Task<VoiceCoverageReply> ExecuteAsync(AudioUpload audio, CancellationToken cancellationToken)
    {
        var transcript = await transcriber.TranscribeAsync(audio, cancellationToken);
        var (status, message) = string.IsNullOrWhiteSpace(transcript)
            ? (CoverageStatus.NoSpeech, "صدای واضحی دریافت نشد. لطفاً نام شهر را دوباره بگویید.")
            : GetReply(matcher.Find(transcript, await repository.GetCitiesAsync(cancellationToken)));

        return new VoiceCoverageReply(status, await synthesizer.SynthesizeAsync(message, cancellationToken));
    }

    private static (CoverageStatus Status, string Message) GetReply(IReadOnlyList<ServiceCity> matches)
    {
        return matches.Count switch
        {
            0 => (CoverageStatus.UnknownCity, "نام شهر را پیدا نکردم. لطفاً نام شهر را واضح‌تر بگویید."),
            > 1 => (CoverageStatus.MultipleCities, "بیش از یک شهر پیدا شد. لطفاً فقط نام یک شهر را بگویید."),
            _ when matches[0].IsActive =>
                (CoverageStatus.Active, $"بله، ما در {matches[0].CityName} خدمات ارائه می‌دهیم."),
            _ => (CoverageStatus.Inactive, $"در حال حاضر در {matches[0].CityName} خدمات ارائه نمی‌دهیم.")
        };
    }
}
