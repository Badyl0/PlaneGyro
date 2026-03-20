using System.Text.Json;
using System.Text.Json.Serialization;

namespace Generated
{
public class Indicators
{
    [JsonPropertyName("valid")] public bool Valid { get; set; }
    [JsonPropertyName("army")] public string Army { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("speed")] public double Speed { get; set; }
    [JsonPropertyName("pedals1")] public double Pedals1 { get; set; }
    [JsonPropertyName("pedals2")] public double Pedals2 { get; set; }
    [JsonPropertyName("pedals3")] public double Pedals3 { get; set; }
    [JsonPropertyName("stick_elevator")] public double StickElevator { get; set; }
    [JsonPropertyName("stick_elevator1")] public double StickElevator1 { get; set; }
    [JsonPropertyName("stick_ailerons")] public double StickAilerons { get; set; }
    [JsonPropertyName("vario")] public double Vario { get; set; }
    [JsonPropertyName("altitude_hour")] public double AltitudeHour { get; set; }
    [JsonPropertyName("bank")] public double Bank { get; set; }
    [JsonPropertyName("turn")] public double Turn { get; set; }
    [JsonPropertyName("compass")] public double Compass { get; set; }
    [JsonPropertyName("clock_hour")] public double ClockHour { get; set; }
    [JsonPropertyName("clock_min")] public double ClockMin { get; set; }
    [JsonPropertyName("manifold_pressure")] public double ManifoldPressure { get; set; }
    [JsonPropertyName("rpm")] public double Rpm { get; set; }
    [JsonPropertyName("oil_pressure")] public double OilPressure { get; set; }
    [JsonPropertyName("oil_temperature")] public double OilTemperature { get; set; }
    [JsonPropertyName("head_temperature")] public double HeadTemperature { get; set; }
    [JsonPropertyName("mixture")] public double Mixture { get; set; }
    [JsonPropertyName("mixture_1")] public double Mixture1 { get; set; }
    [JsonPropertyName("fuel")] public double Fuel { get; set; }
    [JsonPropertyName("fuel_pressure")] public double FuelPressure { get; set; }
    [JsonPropertyName("flaps")] public double Flaps { get; set; }
    [JsonPropertyName("flaps1")] public double Flaps1 { get; set; }
    [JsonPropertyName("flaps2")] public double Flaps2 { get; set; }
    [JsonPropertyName("throttle")] public double Throttle { get; set; }
    [JsonPropertyName("throttle_1")] public double Throttle1 { get; set; }
    [JsonPropertyName("weapon1")] public double Weapon1 { get; set; }
    [JsonPropertyName("weapon3")] public double Weapon3 { get; set; }
    [JsonPropertyName("supercharger")] public double Supercharger { get; set; }
    [JsonPropertyName("blister1")] public double Blister1 { get; set; }
}

}
