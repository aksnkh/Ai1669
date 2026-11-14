using CallCenter.Application.Abstractions;
using CallCenter.Application.Features.VoiceCoverage;
using CallCenter.Domain.Entities;

var cities = new ServiceCity[] {
    new(1, "تهران", "تهران", true, []), new(2, "شیراز", "فارس", false, []),
    new(3, "شهرکرد", "چهارمحال و بختیاری", true, ["شهر کرد"]),
    new(4, "نور", "مازندران", true, []), new(5, "نور آباد", "فارس", false, []),
    new(6, "نور آباد", "لرستان", true, [])
};
int count = 0;
async Task Check(string speech, string status, bool? covered)
{
    var service = new VoiceCoverageService(new Speech(speech), new Cities(cities));
    var result = await service.CheckAsync(Stream.Null, "test.wav", default);
    if (result.Status != status || result.IsCovered != covered || result.Transcript != speech)
        throw new Exception($"Failed: {speech}: {result.Status}");
    count++;
}
await Check("آیا تهران را پوشش می‌دهید؟", "covered", true);
await Check("شيراز", "not_covered", false);
await Check("شهر کرد", "covered", true);
await Check("تهران و شیراز", "multiple_cities", null);
await Check("تهرانپارس", "city_not_found", null);
await Check("سلام", "city_not_found", null);
await Check("", "no_speech", null);
await Check("نور آباد", "multiple_cities", null);
if (CityMatcher.Match("نور آباد", cities).Any(c => c.Id == 4)) throw new Exception("Partial city matched");
count++;
Console.WriteLine($"PASS: {count} voice coverage checks");
sealed class Speech(string text) : ISpeechTranscriber
{
    public Task<string> TranscribeAsync(Stream audio, string fileName, CancellationToken cancellationToken) => Task.FromResult(text);
}
sealed class Cities(IReadOnlyList<ServiceCity> cities) : ICityRepository
{
    public Task<IReadOnlyList<ServiceCity>> GetCitiesAsync(CancellationToken cancellationToken) => Task.FromResult(cities);
}
