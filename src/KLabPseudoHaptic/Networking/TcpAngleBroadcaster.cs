using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using KLabPseudoHaptic.WristAnalysis;

namespace KLabPseudoHaptic.Networking;

/// <summary>
/// Minimal TCP server that broadcasts each produced <see cref="WristAngle"/>
/// as a newline-delimited text line to every connected client.
/// </summary>
/// <remarks>
/// The wire format for each update is
/// <code>&lt;unix-ms&gt;|&lt;degrees&gt;|&lt;classification&gt;\n</code>
/// for example <c>1712345678901|12.50|Extensor\n</c>. The server binds to the
/// IPv4 loopback address by default and is intended for local inter-process
/// communication with other components of the MRI pseudo-haptic pipeline.
/// </remarks>
public sealed class TcpAngleBroadcaster : IAsyncDisposable
{
    private readonly IPEndPoint _configuredEndpoint;
    private readonly ConcurrentDictionary<TcpClient, byte> _clients = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;
    private bool _disposed;

    /// <summary>
    /// Creates a new broadcaster bound to the given port on the loopback address.
    /// Pass <c>0</c> to let the OS pick an ephemeral port.
    /// </summary>
    public TcpAngleBroadcaster(int port, IPAddress? address = null)
    {
        _configuredEndpoint = new IPEndPoint(address ?? IPAddress.Loopback, port);
    }

    /// <summary>
    /// Endpoint the listener is actually bound to. Before <see cref="Start"/>
    /// this is the configured endpoint; afterwards it reflects the real port
    /// (useful when <c>port = 0</c> was requested).
    /// </summary>
    public IPEndPoint Endpoint =>
        _listener?.LocalEndpoint as IPEndPoint ?? _configuredEndpoint;

    /// <summary>Number of currently connected clients.</summary>
    public int ClientCount => _clients.Count;

    /// <summary>Starts listening and accepting clients.</summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_listener is not null)
        {
            return;
        }

        _listener = new TcpListener(_configuredEndpoint);
        _listener.Start();
        _cts = new CancellationTokenSource();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_listener, _cts.Token));
    }

    /// <summary>
    /// Writes the given angle update to every connected client. Clients whose
    /// writes fail are dropped from the broadcast set.
    /// </summary>
    public async Task BroadcastAsync(WristAngle angle, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_listener is null)
        {
            throw new InvalidOperationException("Broadcaster has not been started.");
        }

        var payload = Encoding.UTF8.GetBytes(FormatPayload(angle));

        foreach (var client in _clients.Keys)
        {
            if (!client.Connected)
            {
                DropClient(client);
                continue;
            }

            try
            {
                await client.GetStream().WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                DropClient(client);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _cts?.Cancel();
        }
        catch
        {
            // Ignore
        }

        try
        {
            _listener?.Stop();
        }
        catch
        {
            // Ignore
        }

        if (_acceptLoop is not null)
        {
            try
            {
                await _acceptLoop.ConfigureAwait(false);
            }
            catch
            {
                // AcceptAsync will surface as ObjectDisposedException; swallow.
            }
        }

        foreach (var client in _clients.Keys)
        {
            try
            {
                client.Close();
            }
            catch
            {
                // Ignore
            }
        }
        _clients.Clear();

        _cts?.Dispose();
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException)
            {
                return;
            }

            _clients.TryAdd(client, 0);
        }
    }

    private void DropClient(TcpClient client)
    {
        if (_clients.TryRemove(client, out _))
        {
            try
            {
                client.Close();
            }
            catch
            {
                // Ignore
            }
        }
    }

    internal static string FormatPayload(WristAngle angle)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return string.Create(CultureInfo.InvariantCulture, $"{ts}|{angle.Degrees:F2}|{angle.Classification}\n");
    }
}
