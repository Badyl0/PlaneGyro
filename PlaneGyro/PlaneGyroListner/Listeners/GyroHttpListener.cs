using System.Net;
using PlaneGyroListner.Logging;
using PlaneGyroListner.Models;

namespace PlaneGyroListner.Listeners;

/// <summary>
/// HTTP listener for gyroscope data on localhost:8111.
/// </summary>
internal class GyroHttpListener
{
    private readonly HttpListener _httpListener;
    private readonly GyroDataLogger _dataLogger;
    private const string Prefix = "http://localhost:8111/";
    private const int CheckCancellationMs = 500;
    private const int IdleMessageIntervalMs = 5000;

    public GyroHttpListener()
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(Prefix);
        _dataLogger = new GyroDataLogger();
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
            _dataLogger.Dispose();
        }
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        var lastIdleMessageTime = DateTime.Now;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var getContextTask = _httpListener.GetContextAsync();
                var delayTask = Task.Delay(CheckCancellationMs, cancellationToken);

                var completedTask = await Task.WhenAny(getContextTask, delayTask).ConfigureAwait(false);

                if (completedTask == getContextTask)
                {
                    var context = await getContextTask.ConfigureAwait(false);
                    lastIdleMessageTime = DateTime.Now;
                    _ = HandleRequestAsync(context, cancellationToken);
                }
                else
                {
                    var timeSinceLastMessage = DateTime.Now - lastIdleMessageTime;
                    if (timeSinceLastMessage.TotalMilliseconds >= IdleMessageIntervalMs)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Waiting for API service...");
                        lastIdleMessageTime = DateTime.Now;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting request: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(context.Request.InputStream);
            var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(content))
            {
                var gyroData = System.Text.Json.JsonSerializer.Deserialize<GyroData>(content);
                if (gyroData != null)
                {
                    DisplayGyroData(gyroData);
                    _dataLogger.LogData(gyroData);
                }
            }

            context.Response.StatusCode = 200;
            context.Response.Close();
        }
        catch (System.Text.Json.JsonException ex)
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