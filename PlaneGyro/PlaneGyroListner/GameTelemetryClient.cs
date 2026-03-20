using System.Text.Json;
using PlaneGyroListner.Logging;
using System.Runtime.CompilerServices;

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
    /// Streams raw JSON responses from the endpoint as an async sequence.
    /// Use `await foreach (var raw in client.StreamAsync(..., ct))` to consume.
    /// </summary>
    public async IAsyncEnumerable<string> StreamAsync(
        Uri endpoint,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Console.WriteLine($"Streaming {endpoint} every {_pollIntervalMs}ms");

        while (!cancellationToken.IsCancellationRequested)
        {
            HttpResponseMessage? resp = null;
            try
            {
                resp = await _client.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stream error from {endpoint}: {ex.Message}");
            }

            if (resp != null)
            {
                if (resp.IsSuccessStatusCode)
                {
                    var raw = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    yield return raw;
                }
                else
                {
                    Console.WriteLine($"HTTP {resp.StatusCode} from {endpoint}");
                }
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

