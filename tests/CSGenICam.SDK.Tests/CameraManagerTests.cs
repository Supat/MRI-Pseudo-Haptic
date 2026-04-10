using CSGenICam.SDK;
using CSGenICam.SDK.Exceptions;
using Xunit;

namespace CSGenICam.SDK.Tests;

public class CameraManagerTests
{
    [Fact]
    public void Enumerate_OnEmptySystem_ReturnsEmptyList()
    {
        using var manager = new CameraManager();

        var cameras = manager.Enumerate();

        Assert.Empty(cameras);
    }

    [Fact]
    public void OpenFirst_WhenNoCameras_Throws()
    {
        using var manager = new CameraManager();

        Assert.Throws<CameraNotFoundException>(() => manager.OpenFirst());
    }

    [Fact]
    public void Open_WithNullId_Throws()
    {
        using var manager = new CameraManager();

        Assert.Throws<ArgumentNullException>(() => manager.Open(null!));
    }

    [Fact]
    public void Enumerate_AfterDispose_Throws()
    {
        var manager = new CameraManager();
        manager.Dispose();

        Assert.Throws<ObjectDisposedException>(() => manager.Enumerate());
    }
}
