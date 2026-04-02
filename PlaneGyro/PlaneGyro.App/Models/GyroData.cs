using System.Text.Json.Serialization;

namespace PlaneGyroListner.Models;

/// <summary>
/// Represents gyroscope data from the plane API.
/// </summary>
internal record GyroData(
    [property: JsonPropertyName("x")] float X,
    [property: JsonPropertyName("y")] float Y,
    [property: JsonPropertyName("z")] float Z
);