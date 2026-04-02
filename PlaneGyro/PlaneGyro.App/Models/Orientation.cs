namespace PlaneGyroListner.Models;

/// <summary>
/// Represents aircraft orientation in degrees using a consistent convention.
/// Pitch: positive = nose up, Roll: positive = right wing down, Yaw: positive = nose right.
/// </summary>
public readonly record struct Orientation(
    double PitchDeg,
    double RollDeg,
    double YawDeg
);

