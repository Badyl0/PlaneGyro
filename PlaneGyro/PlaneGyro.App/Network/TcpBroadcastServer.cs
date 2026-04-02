using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PlaneGyroListner.Network;

internal sealed class TcpBroadcastServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly object _clientsLock = new();
    private readonly List<TcpClient> _clients = new();
    private Task? _acceptTask;

    public TcpBroadcastServer(IPAddress bindAddress, int port)
    {
        _listener = new TcpListener(bindAddress, port);
    }

    public int ClientCount
    {
        get { lock (_clientsLock) { return _clients.Count; } }
    }

    public IPEndPoint? LocalEndpoint => _listener.LocalEndpoint as IPEndPoint;

    public void Start(CancellationToken cancellationToken)
    {
        _listener.Start();
        _acceptTask = Task.Run(() => AcceptLoopAsync(cancellationToken), cancellationToken);
    }

    public void Broadcast(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message + "\n");
        List<TcpClient> snapshot;
        lock (_clientsLock)
        {
            snapshot = new List<TcpClient>(_clients);
        }

        foreach (var client in snapshot)
        {
            try
            {
                client.GetStream().Write(bytes, 0, bytes.Length);
            }
            catch
            {
                RemoveClient(client);
            }
        }
    }

    public void Dispose()
    {
        try { _listener.Stop(); } catch { }

        lock (_clientsLock)
        {
            foreach (var c in _clients)
                try { c.Close(); } catch { }
            _clients.Clear();
        }

        try { _acceptTask?.Wait(200); } catch { }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                client.NoDelay = true;
                lock (_clientsLock) { _clients.Add(client); }
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch
            {
                try { client?.Close(); } catch { }
            }
        }
    }

    private void RemoveClient(TcpClient client)
    {
        lock (_clientsLock) { _clients.Remove(client); }
        try { client.Close(); } catch { }
    }
}
