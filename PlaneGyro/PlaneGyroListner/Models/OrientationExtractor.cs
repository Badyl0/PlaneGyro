using System.Text.Json;

namespace PlaneGyroListner.Models;

/// <summary>
/// Helper methods to derive Orientation from raw JSON or strongly-typed state models.
/// </summary>
public static class OrientationExtractor
{
    /// <summary>
    /// Builds an Orientation from a JSON payload that exposes x/y/z angles in degrees.
    /// Falls back to zeros when properties are missing or not numbers.
    /// </summary>
    public static Orientation FromJson(JsonElement root)
    {
        double pitch = 0;
        double roll = 0;
        double yaw = 0;

        if (root.TryGetProperty("x", out var xProp) && xProp.TryGetDouble(out var x))
        {
            pitch = x;
        }

        if (root.TryGetProperty("y", out var yProp) && yProp.TryGetDouble(out var y))
        {
            roll = y;
        }

        if (root.TryGetProperty("z", out var zProp) && zProp.TryGetDouble(out var z))
        {
            yaw = z;
        }

        return new Orientation(pitch, roll, yaw);
    }

    /// <summary>
    /// Builds an Orientation from the /state model using available attitude-related fields.
    /// Current mapping: pitch from Pitch1Deg, yaw from AngleOfSideslipDeg, roll assumed 0.
    /// This can be refined later as we understand the telemetry better.
    /// </summary>
    public static Orientation FromState(State state)
    {
        var pitch = state.Pitch1Deg;
        var roll = 0.0;
        var yaw = state.AngleOfSideslipDeg;

        return new Orientation(pitch, roll, yaw);
    }
}

