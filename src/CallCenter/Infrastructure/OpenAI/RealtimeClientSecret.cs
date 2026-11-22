using System.Text.Json.Serialization;

namespace CallCenter.Infrastructure.OpenAI;

public sealed record RealtimeClientSecret(
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("expires_at")] long ExpiresAt);
