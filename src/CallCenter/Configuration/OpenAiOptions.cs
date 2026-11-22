namespace CallCenter.Configuration;

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = "";
    public string TranscriptionModel { get; set; } = "gpt-4o-transcribe";
    public string? RealtimeTranscriptionModel { get; set; }
    public string CityRecognitionModel { get; set; } = "gpt-4.1-mini";
    public string SpeechModel { get; set; } = "gpt-4o-mini-tts";
    public string Voice { get; set; } = "nova";
}
