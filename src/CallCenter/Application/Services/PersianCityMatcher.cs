using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CallCenter.Domain.Entities;

namespace CallCenter.Application.Services;

public sealed partial class PersianCityMatcher
{
    public IReadOnlyList<ServiceCity> Find(string transcript, IReadOnlyList<ServiceCity> cities)
    {
        var normalized = $" {Normalize(transcript)} ";
        return cities.Where(city => ContainsCity(normalized, city.CityName)).ToArray();
    }

    private static bool ContainsCity(string transcript, string cityName)
    {
        var name = Normalize(cityName);
        if (name.Length == 0)
            return false;

        return transcript.Contains($" {name} ", StringComparison.Ordinal)
            || (name == "بندرعباس" && transcript.Contains(" بندر عباس ", StringComparison.Ordinal));
    }

    private static string Normalize(string value)
    {
        var letters = new StringBuilder();
        foreach (var character in value.Normalize(NormalizationForm.FormKC))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark
                || character is 'ـ' or '\u200e' or '\u200f' or '\u200d')
                continue;

            letters.Append(character switch
            {
                'ي' or 'ى' => 'ی',
                'ك' => 'ک',
                'أ' or 'إ' or 'ٱ' => 'ا',
                _ => character
            });
        }

        return WordSeparators().Replace(letters.ToString(), " ").Trim();
    }

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex WordSeparators();
}
