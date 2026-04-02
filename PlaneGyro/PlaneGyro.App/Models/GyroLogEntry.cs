using System.Text.Json.Serialization;

namespace PlaneGyroListner.Models;

/// <summary>
/// Represents a single gyroscope data log entry.
/// </summary>
internal record GyroLogEntry(
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("x")] float X,
    [property: JsonPropertyName("y")] float Y,
    [property: JsonPropertyName("z")] float Z
);