using System.IO.Ports;
using System.Text.Json;
using PlaneGyroListner;
using PlaneGyroListner.Logging;
using PlaneGyroListner.Models;

// "HttpListener" on the same port is intentionally removed because another service
// is already exposing JSON on that port. Use the HTTP poller below to read data
// from the existing service instead of starting a listener that would conflict.

var logToFile = args.Any(a =>
    string.Equals(a, "--logToFile", StringComparison.OrdinalIgnoreCase));

var orientationDemo = args.Any(a =>
    string.Equals(a, "--orientation-demo", StringComparison.OrdinalIgnoreCase));

var testMode = args.Any(a =>
    string.Equals(a, "--test-mode", StringComparison.OrdinalIgnoreCase));

var serialPortArg = args.FirstOrDefault(a =>
    a.StartsWith("--serial-port=", StringComparison.OrdinalIgnoreCase));
var serialPortName = serialPortArg is null
    ? null
    : serialPortArg.Substring("--serial-port=".Length);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

if (testMode)
{
    if (string.IsNullOrWhiteSpace(serialPortName))
    {
        Console.WriteLine("Test mode requires --serial-port=COMx argument.");
        return;
    }

    await RunSerialTestModeAsync(serialPortName, cts.Token);
    return;
}

GyroDataLogger? indicatorsLogger = null;
GyroDataLogger? stateLogger = null;

if (logToFile)
{
    indicatorsLogger = new GyroDataLogger("indicators");
    stateLogger = new GyroDataLogger("state");
}

using var telemetryClient = new GameTelemetryClient(pollIntervalMs: 100);

var indicatorsEndpoint = new Uri("http://localhost:8111/indicators");
var stateEndpoint = new Uri("http://localhost:8111/state");

Task indicatorsTask;
Task stateTask;
SerialPort? port = null;
object portLock = new object();
string? latestIndicatorsRaw = null;
string? latestStateRaw = null;
object latestLock = new object();

if (!string.IsNullOrWhiteSpace(serialPortName))
{
    port = new SerialPort(serialPortName, 115200)
    {
        NewLine = "\n",
        DtrEnable = true,
        RtsEnable = true
    };

    try
    {
        port.Open();
        Console.WriteLine($"Serial port {serialPortName} opened.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to open serial port {serialPortName}: {ex.Message}");
        port = null;
    }
}

// consume telemetry streams and update latest raw values
indicatorsTask = Task.Run(async () =>
{
    await foreach (var raw in telemetryClient.StreamAsync(indicatorsEndpoint, cts.Token).ConfigureAwait(false))
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Raw JSON received from {indicatorsEndpoint} ({raw.Length} chars)");

        if (logToFile)
        {
            indicatorsLogger!.LogRawJson(raw);
        }

        lock (latestLock)
        {
            latestIndicatorsRaw = raw;
        }
    }
}, cts.Token);

stateTask = Task.Run(async () =>
{
    await foreach (var raw in telemetryClient.StreamAsync(stateEndpoint, cts.Token).ConfigureAwait(false))
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Raw JSON received from {stateEndpoint} ({raw.Length} chars)");

        if (logToFile)
        {
            stateLogger!.LogRawJson(raw);
        }

        lock (latestLock)
        {
            latestStateRaw = raw;
        }
    }
}, cts.Token);

// start a sender loop that composes payloads and writes to serial port
Task? senderTask = null;
if (port is not null)
{
    senderTask = Task.Run(async () =>
    {
        var interval = 100;
        while (!cts.Token.IsCancellationRequested)
        {
            string? indRaw;
            string? stRaw;
            lock (latestLock)
            {
                indRaw = latestIndicatorsRaw;
                stRaw = latestStateRaw;
            }

            double pitch = 0, roll = 0, yaw = 0;
            int flaps = 0;
            int gear = 0; // default Up

            // try indicators first for orientation and flaps
            if (!string.IsNullOrWhiteSpace(indRaw))
            {
                try
                {
                    using var doc = JsonDocument.Parse(indRaw);
                    var orientation = OrientationExtractor.FromJson(doc.RootElement);
                    pitch = orientation.PitchDeg;
                    roll = orientation.RollDeg;
                    yaw = orientation.YawDeg;
                }
                catch (JsonException)
                {
                }
            }

            // if state available, prefer its orientation mapping and flaps
            if (!string.IsNullOrWhiteSpace(stRaw))
            {
                // Try to deserialize to State if possible
                try
                {
                    State state = JsonSerializer.Deserialize<State>(stRaw);
                    flaps = FlapsExtractor.FromPercent(state.FlapsPercent);
                    gear = GearExtractor.FromPercent(state.GearPercent);
                }
                catch
                {
                    // ignore deserialization errors
                }               
            }

            var payload = new { pitch, roll, yaw, flaps, gear };
            var json = JsonSerializer.Serialize(payload);

            try
            {
                lock (portLock)
                {
                    port.WriteLine(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write payload to serial port: {ex.Message}");
            }

            try
            {
                await Task.Delay(interval, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }, cts.Token);
}

// run both pollers and optional sender concurrently until cancellation
var tasks = new List<Task> { indicatorsTask, stateTask };
if (senderTask is not null) tasks.Add(senderTask);
await Task.WhenAll(tasks);

indicatorsLogger?.Dispose();
stateLogger?.Dispose();
if (port is not null)
{
    lock (portLock)
    {
        try { port.Close(); } catch { }
    }
}

static async Task RunSerialTestModeAsync(string serialPortName, CancellationToken cancellationToken)
{
    Console.WriteLine($"Test mode enabled. Using serial port: {serialPortName}");

    // Locate TestData directory relative to the build output directory.
    // TestData is copied next to the executable by the csproj settings.
    var baseDir = AppContext.BaseDirectory;
    var testDataDirectory = Path.Combine(baseDir, "TestData");
    var stateFilePath = Path.Combine(testDataDirectory, "state_data_2026-03-10_23-33-33.jsonl");

    if (!File.Exists(stateFilePath))
    {
        Console.WriteLine($"State data file not found at {stateFilePath}");
        return;
    }

    // Read the file as a stream and deserialize State objects. This uses
    // DeserializeAsyncEnumerable which streams a top-level JSON array (or single
    // value). It's a simpler approach — if the input is not a JSON array this
    // may fail, but it's concise and efficient for typical JSON files.
    var orientationMessages = new List<string>();
    int tmpFlaps = 0;
    int lineIndex = 0;

    try
    {
        await using var fs = File.OpenRead(stateFilePath);
        await foreach (var state in JsonSerializer.DeserializeAsyncEnumerable<State?>(fs).ConfigureAwait(false))
        {
            if (state is null)
            {
                continue;
            }

            lineIndex++;
            if (lineIndex < 50)
            {
                tmpFlaps = 0;
            }
            else if (lineIndex > 50 && lineIndex < 100)
            {
                tmpFlaps = 1;
            }
            else if (lineIndex > 100 && lineIndex < 150)
            {
                tmpFlaps = 2;
            }
            else if (lineIndex > 150)
            {
                tmpFlaps = 3;
            }

            var orientation = OrientationExtractor.FromState(state.Value);

            var payload = new
            {
                pitch = 0.0,
                roll = 0.0,
                yaw = 0.0,
                flaps = tmpFlaps
            };

            var json = JsonSerializer.Serialize(payload);
            orientationMessages.Add(json);
        }
    }
    catch (JsonException)
    {
        // If the JSON is malformed or not in an expected shape, fall through and report no samples.
    }

    if (orientationMessages.Count == 0)
    {
        Console.WriteLine("No valid state samples found in TestData file.");
        return;
    }

    Console.WriteLine($"Loaded {orientationMessages.Count} orientation samples from TestData.");

    using var port = new SerialPort(serialPortName, 115200)
    {
        NewLine = "\n",
        DtrEnable = true,
        RtsEnable = true
    };

    try
    {
        port.Open();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to open serial port {serialPortName}: {ex.Message}");
        return;
    }

    Console.WriteLine("Serial port opened. Replaying orientation JSON sequence until cancelled (Ctrl+C).");

    try
    {
        var index = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var message = orientationMessages[index];
            port.WriteLine(message);
            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss.fff}] Sent: {message}");

            index++;
            if (index >= orientationMessages.Count)
            {
                index = 0;
            }

            try
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
    finally
    {
        if (port.IsOpen)
        {
            port.Close();
        }
        Console.WriteLine("Test mode stopped.");
    }
}

