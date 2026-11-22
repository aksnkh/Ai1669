namespace CallCenter.Infrastructure.Speech;

public sealed class SpeechOptions
{
    public Uri BaseUrl { get; set; } = new("http://127.0.0.1:8000/");
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);
}
