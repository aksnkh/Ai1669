namespace CallCenter.Application.Features.VoiceCoverage;
public sealed record CityMatch(int Id, string Name, string? Province, bool IsCovered);
public sealed record VoiceCoverageResult(string Status, string Transcript, bool? IsCovered,
    string Message, IReadOnlyList<CityMatch> Cities);
