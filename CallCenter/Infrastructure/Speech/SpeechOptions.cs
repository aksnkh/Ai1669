namespace CallCenter.Infrastructure.Speech;
public sealed class SpeechOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "whisper-1";
}
