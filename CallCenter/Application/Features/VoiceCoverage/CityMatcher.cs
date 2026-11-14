using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CallCenter.Domain.Entities;
namespace CallCenter.Application.Features.VoiceCoverage;
public static class CityMatcher
{
    public static string Normalize(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC).Replace('ي', 'ی').Replace('ى', 'ی').Replace('ك', 'ک');
        var text = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark || ch == 'ـ') continue;
            text.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ');
        }
        return Regex.Replace(text.ToString(), @"\s+", " ").Trim();
    }

    public static IReadOnlyList<CityMatch> Match(string transcript, IReadOnlyList<ServiceCity> cities)
    {
        var text = " " + Normalize(transcript) + " ";
        var hits = new List<(ServiceCity City, int Start, int End)>();
        foreach (var city in cities)
        foreach (var name in city.Aliases.Prepend(city.Name).Distinct())
        {
            var normalized = Normalize(name);
            if (normalized.Length == 0) continue;
            var pattern = " " + normalized + " ";
            for (var start = text.IndexOf(pattern, StringComparison.Ordinal); start >= 0;
                 start = text.IndexOf(pattern, start + 1, StringComparison.Ordinal))
                hits.Add((city, start, start + pattern.Length));
        }
        // Prefer a full city name over another city's name contained within it.
        return hits.Where(hit => !hits.Any(other => other.Start <= hit.Start && other.End >= hit.End
                && other.End - other.Start > hit.End - hit.Start))
            .Select(hit => hit.City).DistinctBy(city => city.Id)
            .Select(city => new CityMatch(city.Id, city.Name, city.Province, city.IsCovered)).ToArray();
    }
}
