using CallCenter.Application.Abstractions;
namespace CallCenter.Application.Features.VoiceCoverage;
public sealed class VoiceCoverageService(ISpeechTranscriber transcriber, ICityRepository repository)
{
    public async Task<VoiceCoverageResult> CheckAsync(Stream audio, string fileName, CancellationToken cancellationToken)
    {
        var transcript = await transcriber.TranscribeAsync(audio, fileName, cancellationToken);
        if (string.IsNullOrWhiteSpace(transcript))
            return new("no_speech", transcript, null, "گفتاری تشخیص داده نشد؛ لطفاً دوباره تلاش کنید.", []);
        var matches = CityMatcher.Match(transcript, await repository.GetCitiesAsync(cancellationToken));
        if (matches.Count == 0)
            return new("city_not_found", transcript, null, "نام شهر در فهرست شهرها پیدا نشد؛ لطفاً نام شهر را واضح بگویید.", matches);
        if (matches.Count > 1)
            return new("multiple_cities", transcript, null, "چند شهر یا نام مبهم شناسایی شد؛ لطفاً فقط یک شهر را مشخص کنید.", matches);
        var city = matches[0];
        return new(city.IsCovered ? "covered" : "not_covered", transcript, city.IsCovered,
            city.IsCovered ? $"بله، شهر {city.Name} تحت پوشش خدمات ما است." : $"خیر، شهر {city.Name} تحت پوشش خدمات ما نیست.", matches);
    }
}
