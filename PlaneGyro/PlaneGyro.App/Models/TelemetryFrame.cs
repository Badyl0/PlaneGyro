namespace PlaneGyroListner.Models;

/// <summary>
/// Composed telemetry frame emitted by FrameCompositionService once per cycle.
/// All angle values are in degrees. Vario is in m/s.
/// </summary>
public record TelemetryFrame(
    double Pitch,
    double Roll,
    double Yaw,
    double Vario,
    double Bank,
    double Turn,
    int Flaps,
    int Gear,
    DateTime Timestamp,
    bool ConnectionOk
)
{
    public static TelemetryFrame Empty { get; } = new(
        Pitch: 0, Roll: 0, Yaw: 0,
        Vario: 0, Bank: 0, Turn: 0,
        Flaps: 0, Gear: 0,
        Timestamp: DateTime.MinValue,
        ConnectionOk: false
    );
}
