using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

const string ListenerAddress = "http://localhost:8111";
var listener = new GyroHttpListener();

await listener.StartAsync();

/// <summary>
/// HTTP listener for gyroscope data on localhost:8111.
/// </summary>
internal class GyroHttpListener
{
    private readonly HttpListener _httpListener;
    private const string Prefix = "http://localhost:8111/";

    public GyroHttpListener()
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(Prefix);
    }

    public async Task StartAsync()
    {
        Console.WriteLine("PlaneGyroListener started...");
        Console.WriteLine($"Listening on {Prefix}");
        Console.WriteLine("Press Ctrl+C to exit\n");

        _httpListener.Start();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await ListenAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nListener stopped gracefully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            _httpListener.Stop();
            _httpListener.Close();
        }
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await _httpListener.GetContextAsync().ConfigureAwait(false);
                _ = HandleRequestAsync(context, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995)
            {
                // Operation aborted - listener was stopped
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting request: {ex.Message}");
            }
        }
    }

    private static async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(context.Request.InputStream);
            var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(content))
            {
                var gyroData = JsonSerializer.Deserialize<GyroData>(content);
                if (gyroData != null)
                {
                    DisplayGyroData(gyroData);
                }
            }

            context.Response.StatusCode = 200;
            context.Response.Close();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON parse error: {ex.Message}");
            context.Response.StatusCode = 400;
            context.Response.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request handling error: {ex.Message}");
            context.Response.StatusCode = 500;
            context.Response.Close();
        }
    }

    private static void DisplayGyroData(GyroData data)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Gyro - X: {data.X:F2}° Y: {data.Y:F2}° Z: {data.Z:F2}°");
    }
}

/// <summary>
/// Represents gyroscope data from the plane API.
/// </summary>
internal record GyroData(
    [property: JsonPropertyName("x")] float X,
    [property: JsonPropertyName("y")] float Y,
    [property: JsonPropertyName("z")] float Z
);
