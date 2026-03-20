using System.Text;
using System.Text.Json;
using PlaneGyroListner.Models;

namespace PlaneGyroListner.Logging;

/// <summary>
/// Logs gyroscope data to a JSON Lines file with 100ms sampling period.
/// </summary>
internal class GyroDataLogger : IDisposable
{
    private readonly string _logFilePath;
    private readonly StreamWriter _writer;
    private DateTime _lastLogTime;
    private const int SamplingIntervalMs = 100;
    // _generatedModel removed: do not auto-generate or overwrite manual model file

    public GyroDataLogger(string prefix = "gyro")
    {
        var testDataDirectory = Path.Combine(AppContext.BaseDirectory, "..", "..", "TestData");
        var logsDirectory = Path.Combine(testDataDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        // include prefix in filename so different endpoints write to separate files
        var safePrefix = string.IsNullOrWhiteSpace(prefix) ? "gyro" : prefix;
        _logFilePath = Path.Combine(logsDirectory, $"{safePrefix}_data_{timestamp}.jsonl");

        _writer = new StreamWriter(_logFilePath, append: true, Encoding.UTF8, bufferSize: 4096);
        _lastLogTime = DateTime.Now;

        Console.WriteLine($"Data logging started: {_logFilePath}\n");
    }

    public void LogData(GyroData data)
    {
        var now = DateTime.Now;
        var timeSinceLastLog = (now - _lastLogTime).TotalMilliseconds;

        if (timeSinceLastLog >= SamplingIntervalMs)
        {
            var logEntry = new GyroLogEntry(now, data.X, data.Y, data.Z);
            var json = JsonSerializer.Serialize(logEntry);
            _writer.WriteLine(json);
            _lastLogTime = now;
        }
    }

    public void LogOrientation(Orientation orientation)
    {
        var data = new GyroData(
            (float)orientation.PitchDeg,
            (float)orientation.RollDeg,
            (float)orientation.YawDeg);

        LogData(data);
    }

    public void LogRawJson(string rawJson)
    {
        var now = DateTime.Now;
        var timeSinceLastLog = (now - _lastLogTime).TotalMilliseconds;

        if (timeSinceLastLog >= SamplingIntervalMs)
        {
            // store raw JSON as-is (one JSON per line). Do not modify Generated/IndicatorModel.cs
            _writer.WriteLine(rawJson);
            _lastLogTime = now;
        }
    }

    public void Dispose()
    {
        _writer?.Flush();
        _writer?.Dispose();
        try
        {
            var fullPath = Path.GetFullPath(_logFilePath);
            Console.WriteLine($"Data logging stopped. File saved: {fullPath}");
        }
        catch
        {
            Console.WriteLine($"Data logging stopped. File saved: {_logFilePath}");
        }
    }
}