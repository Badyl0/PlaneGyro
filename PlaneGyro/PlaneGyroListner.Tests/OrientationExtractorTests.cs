using System.Text.Json;
using PlaneGyroListner.Models;
using Xunit;

namespace PlaneGyroListner.Tests;

public class OrientationExtractorTests
{
    [Fact]
    public void FromJson_WhenXyzPresent_MapsToPitchRollYaw()
    {
        // Arrange
        var json = "{\"x\":10.5,\"y\":-2.25,\"z\":45.0}";
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Act
        var orientation = OrientationExtractor.FromJson(root);

        // Assert
        Assert.Equal(10.5, orientation.PitchDeg, 3);
        Assert.Equal(-2.25, orientation.RollDeg, 3);
        Assert.Equal(45.0, orientation.YawDeg, 3);
    }

    [Fact]
    public void FromState_UsesAngleFieldsWhenAvailable()
    {
        // Arrange
        var state = new State(
            Valid: true,
            AileronPercent: 0,
            ElevatorPercent: 0,
            RudderPercent: 0,
            FlapsPercent: 0,
            AltitudeMeters: 1000,
            TrueAirspeedKmh: 300,
            IndicatedAirspeedKmh: 280,
            Mach: 0.5,
            AngleOfAttackDeg: 3.0,
            AngleOfSideslipDeg: -1.5,
            Ny: 1.0,
            VerticalSpeed: 0,
            WxDegPerSec: 0,
            MfuelKg: 100,
            Mfuel0Kg: 150,
            Throttle1Percent: 80,
            RpmThrottle1Percent: 80,
            Mixture1Percent: 100,
            Radiator1Percent: 50,
            Magneto1: 1,
            Power1Hp: 1000,
            Rpm1: 2500,
            ManifoldPressure1Atm: 1.0,
            OilTemp1C: 80,
            Pitch1Deg: 10.0,
            Thrust1Kgs: 500,
            Efficiency1Percent: 90
        );

        // Act
        var orientation = OrientationExtractor.FromState(state);

        // Assert
        Assert.Equal(10.0, orientation.PitchDeg, 3);
        Assert.Equal(0.0, orientation.RollDeg, 3);
        Assert.Equal(-1.5, orientation.YawDeg, 3);
    }
}

