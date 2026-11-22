namespace CallCenter.Infrastructure.Audio;

public static class AudioFormats
{
    public const long MaxAudioBytes = 24 * 1024 * 1024;
    public const long MaxRequestBytes = MaxAudioBytes + 1024 * 1024;

    private static readonly Dictionary<string, string> MediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".wav"] = "audio/wav",
        [".mp3"] = "audio/mpeg",
        [".mpga"] = "audio/mpeg",
        [".mpeg"] = "audio/mpeg",
        [".m4a"] = "audio/mp4",
        [".mp4"] = "video/mp4",
        [".webm"] = "audio/webm",
        [".flac"] = "audio/flac",
        [".ogg"] = "audio/ogg"
    };

    public static string? MediaTypeFor(string fileName) => MediaTypes.GetValueOrDefault(Path.GetExtension(fileName));
}
