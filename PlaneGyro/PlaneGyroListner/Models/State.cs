using System.Text.Json.Serialization;

namespace PlaneGyroListner.Models;

/// <summary>
/// POCO generated from TestData sample for the /state endpoint.
/// Property names map to original JSON keys using JsonPropertyName.
/// </summary>
public readonly record struct State(
    [property: JsonPropertyName("valid")] bool Valid,
    [property: JsonPropertyName("aileron, %")] double AileronPercent,
    [property: JsonPropertyName("elevator, %")] double ElevatorPercent,
    [property: JsonPropertyName("rudder, %")] double RudderPercent,
    [property: JsonPropertyName("flaps, %")] double FlapsPercent,
    [property: JsonPropertyName("H, m")] int AltitudeMeters,
    [property: JsonPropertyName("TAS, km/h")] double TrueAirspeedKmh,
    [property: JsonPropertyName("IAS, km/h")] double IndicatedAirspeedKmh,
    [property: JsonPropertyName("M")] double Mach,
    [property: JsonPropertyName("AoA, deg")] double AngleOfAttackDeg,
    [property: JsonPropertyName("AoS, deg")] double AngleOfSideslipDeg,
    [property: JsonPropertyName("Ny")] double Ny,
    [property: JsonPropertyName("Vy, m/s")] double VerticalSpeed,
    [property: JsonPropertyName("Wx, deg/s")] double WxDegPerSec,
    [property: JsonPropertyName("Mfuel, kg")] double MfuelKg,
    [property: JsonPropertyName("Mfuel0, kg")] double Mfuel0Kg,
    [property: JsonPropertyName("throttle 1, %")] double Throttle1Percent,
    [property: JsonPropertyName("RPM throttle 1, %")] double RpmThrottle1Percent,
    [property: JsonPropertyName("mixture 1, %")] double Mixture1Percent,
    [property: JsonPropertyName("radiator 1, %")] double Radiator1Percent,
    [property: JsonPropertyName("magneto 1")] int Magneto1,
    [property: JsonPropertyName("power 1, hp")] double Power1Hp,
    [property: JsonPropertyName("RPM 1")] int Rpm1,
    [property: JsonPropertyName("manifold pressure 1, atm")] double ManifoldPressure1Atm,
    [property: JsonPropertyName("oil temp 1, C")] double OilTemp1C,
    [property: JsonPropertyName("pitch 1, deg")] double Pitch1Deg,
    [property: JsonPropertyName("thrust 1, kgs")] double Thrust1Kgs,
    [property: JsonPropertyName("efficiency 1, %")] double Efficiency1Percent
);
