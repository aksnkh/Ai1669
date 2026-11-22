namespace CallCenter.Configuration;

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = "";
    public string TranscriptionModel { get; set; } = "gpt-4o-transcribe";
    public string SpeechModel { get; set; } = "gpt-4o-mini-tts";
    public string Voice { get; set; } = "nova";
}
