using CleanHome.Application.Abstractions.Verification;
using CleanHome.Infrastructure.Security;

namespace CleanHome.UnitTests.Security;

public sealed class LocalContentScanServiceTests
{
    private readonly LocalContentScanService _service = new();

    [Fact]
    public async Task ScanAsync_WithContent_ReturnsClean()
    {
        using var content = new MemoryStream(new byte[]
        {
            0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34,
            0x0A, 0x25, 0x50, 0x44, 0x46, 0x2D, 0x32, 0x36, 0x0A
        });

        var result = await _service.ScanAsync(content);

        Assert.Equal(ContentScanStatus.Clean, result.Status);
        Assert.Null(result.Reason);
    }

    [Fact]
    public async Task ScanAsync_WithEmptyContent_ReturnsPending()
    {
        using var content = new MemoryStream(Array.Empty<byte>());

        var result = await _service.ScanAsync(content);

        Assert.Equal(ContentScanStatus.Pending, result.Status);
        Assert.NotNull(result.Reason);
    }
}
