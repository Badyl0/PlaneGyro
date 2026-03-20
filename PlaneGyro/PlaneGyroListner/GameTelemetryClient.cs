using System.Text.Json;
using PlaneGyroListner.Logging;

namespace PlaneGyroListner;

/// <summary>
/// Polls the game HTTP telemetry endpoints at a fixed interval and forwards raw JSON
/// to a caller-supplied handler. This isolates HTTP concerns from logging and processing.
/// </summary>
internal sealed class GameTelemetryClient : IDisposable
{
    private readonly HttpClient _client = new();
    private readonly int _pollIntervalMs;

    public GameTelemetryClient(int pollIntervalMs = 100)
    {
        _pollIntervalMs = pollIntervalMs;
    }

    /// <summary>
    /// Starts polling the specified endpoint until cancellation is requested.
    /// For each successful response, invokes <paramref name="onJson"/> with the raw JSON body.
    /// </summary>
    public async Task StartPollingAsync(
        Uri endpoint,
        Action<string> onJson,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Polling {endpoint} every {_pollIntervalMs}ms");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var resp = await _client.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
                if (resp.IsSuccessStatusCode)
                {
                    var raw = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    onJson(raw);
                }
                else
                {
                    Console.WriteLine($"HTTP {resp.StatusCode} from {endpoint}");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Poll error from {endpoint}: {ex.Message}");
            }

            try
            {
                await Task.Delay(_pollIntervalMs, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}

