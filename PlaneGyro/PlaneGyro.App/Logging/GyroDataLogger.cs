using System.IO;
using System.Text.Json;
using PlaneGyroListner.Models;

namespace PlaneGyroListner.Logging;

internal class GyroDataLogger : IDisposable
{
    private readonly string _logFilePath;
    private readonly FileStream _fileStream;
    private readonly Utf8JsonWriter _jsonWriter;
    private DateTime _lastLogTime;
    private const int SamplingIntervalMs = 100;

    public GyroDataLogger(string prefix = "gyro")
    {
        var testDataDirectory = Path.Combine(AppContext.BaseDirectory, "..", "..", "TestData");
        var logsDirectory = Path.Combine(testDataDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var safePrefix = string.IsNullOrWhiteSpace(prefix) ? "gyro" : prefix;
        _logFilePath = Path.Combine(logsDirectory, $"{safePrefix}_data_{timestamp}.json");

        _fileStream = new FileStream(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096);
        _jsonWriter = new Utf8JsonWriter(_fileStream, new JsonWriterOptions { Indented = false });
        _jsonWriter.WriteStartArray();
        _lastLogTime = DateTime.Now;

        Console.WriteLine($"Data logging started: {_logFilePath}\n");
    }

    public void LogData(GyroData data)
    {
        var now = DateTime.Now;
        if ((now - _lastLogTime).TotalMilliseconds >= SamplingIntervalMs)
        {
            var logEntry = new GyroLogEntry(now, data.X, data.Y, data.Z);
            JsonSerializer.Serialize(_jsonWriter, logEntry);
            _jsonWriter.Flush();
            _lastLogTime = now;
        }
    }

    public void LogOrientation(Orientation orientation)
    {
        LogData(new GyroData(
            (float)orientation.PitchDeg,
            (float)orientation.RollDeg,
            (float)orientation.YawDeg));
    }

    public void LogRawJson(string rawJson)
    {
        var now = DateTime.Now;
        if ((now - _lastLogTime).TotalMilliseconds >= SamplingIntervalMs)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                doc.RootElement.WriteTo(_jsonWriter);
                _jsonWriter.Flush();
            }
            catch (JsonException) { /* skip malformed JSON */ }
            _lastLogTime = now;
        }
    }

    public void Dispose()
    {
        _jsonWriter.WriteEndArray();
        _jsonWriter.Flush();
        _jsonWriter.Dispose();
        _fileStream.Dispose();
        try
        {
            Console.WriteLine($"Data logging stopped. File saved: {Path.GetFullPath(_logFilePath)}");
        }
        catch
        {
            Console.WriteLine($"Data logging stopped. File saved: {_logFilePath}");
        }
    }
}