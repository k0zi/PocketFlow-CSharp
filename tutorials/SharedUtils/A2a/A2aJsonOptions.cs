using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2aProtocol;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> used by both the A2A client and server.
/// </summary>
public static class A2aJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        },
    };

    /// <summary>Pretty-printed variant for human-readable output.</summary>
    public static readonly JsonSerializerOptions Pretty = new(Default) { WriteIndented = true };
}

