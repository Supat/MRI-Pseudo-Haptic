using System.Net;
using System.Net.Sockets;
using System.Text;
using KLabPseudoHaptic.Networking;
using KLabPseudoHaptic.WristAnalysis;
using Xunit;

namespace KLabPseudoHaptic.Tests;

public class TcpAngleBroadcasterTests
{
    [Fact]
    public async Task Start_BindsToLoopbackOnRequestedPort()
    {
        await using var broadcaster = new TcpAngleBroadcaster(port: 0);
        broadcaster.Start();

        Assert.Equal(IPAddress.Loopback, broadcaster.Endpoint.Address);
        Assert.NotEqual(0, broadcaster.Endpoint.Port);
    }

    [Fact]
    public async Task BroadcastAsync_SendsFormattedLineToConnectedClient()
    {
        await using var broadcaster = new TcpAngleBroadcaster(port: 0);
        broadcaster.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(broadcaster.Endpoint.Address, broadcaster.Endpoint.Port);

        await WaitForClientRegistered(broadcaster);

        var angle = new WristAngle(12.34, WristClassification.Extensor);
        await broadcaster.BroadcastAsync(angle);

        using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
        var line = await ReadLineWithTimeout(reader, TimeSpan.FromSeconds(2));

        Assert.NotNull(line);
        var parts = line!.Split('|');
        Assert.Equal(3, parts.Length);
        Assert.True(long.TryParse(parts[0], out _));
        Assert.Equal("12.34", parts[1]);
        Assert.Equal("Extensor", parts[2]);
    }

    [Fact]
    public async Task BroadcastAsync_BeforeStart_Throws()
    {
        await using var broadcaster = new TcpAngleBroadcaster(port: 0);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            broadcaster.BroadcastAsync(new WristAngle(0, WristClassification.Neutral)));
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent()
    {
        var broadcaster = new TcpAngleBroadcaster(port: 0);
        broadcaster.Start();

        await broadcaster.DisposeAsync();
        await broadcaster.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            broadcaster.BroadcastAsync(new WristAngle(0, WristClassification.Neutral)));
    }

    private static async Task WaitForClientRegistered(TcpAngleBroadcaster broadcaster)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (broadcaster.ClientCount == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }
        Assert.True(broadcaster.ClientCount > 0, "client did not register within timeout");
    }

    private static async Task<string?> ReadLineWithTimeout(StreamReader reader, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var task = reader.ReadLineAsync(cts.Token).AsTask();
        return await task;
    }
}
