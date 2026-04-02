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

        if (root.TryGetProperty("vario", out var varioProp) && varioProp.TryGetDouble(out var vario))
        {
            // Scale vario [-20, 20] m/s to pitch [-90, 90] degrees
            pitch = Math.Clamp(vario / 20.0 * 90.0, -90.0, 90.0);
        }

        if (root.TryGetProperty("y", out var yProp) && yProp.TryGetDouble(out var y))
        {
            roll = y;
        }

        if (root.TryGetProperty("bank", out var bankProp) && bankProp.TryGetDouble(out var bank))
        {
            // Scale bank [-10, 10] to roll [-90, 90] degrees
            roll = Math.Clamp(bank / 10.0 * 90.0, -90.0, 90.0);
        }

        if (root.TryGetProperty("z", out var zProp) && zProp.TryGetDouble(out var z))
        {
            yaw = z;
        }

        if (root.TryGetProperty("turn", out var turnProp) && turnProp.TryGetDouble(out var turn))
        {
            // Scale turn [-10, 10] to yaw [-90, 90] degrees
            yaw = Math.Clamp(turn / 10.0 * 90.0, -90.0, 90.0);
        }

        return new Orientation(pitch, roll, yaw);
    }

    /// <summary>
    /// Builds an Orientation from the /state model using available attitude-related fields.
    /// Current mapping: pitch from AngleOfAttackDeg (AoA), yaw from AngleOfSideslipDeg, roll assumed 0.
    /// This can be refined later as we understand the telemetry better.
    /// </summary>
    public static Orientation FromState(State state)
    {
        var pitch = state.AngleOfAttackDeg;
        var roll = 0.0;
        var yaw = state.AngleOfSideslipDeg;

        return new Orientation(pitch, roll, yaw);
    }
}

