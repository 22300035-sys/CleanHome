using CleanHome.Application.Abstractions.Verification;
using CleanHome.Infrastructure.Security;
using CleanHome.Infrastructure.Validation;

namespace CleanHome.UnitTests.Security;

public sealed class LocalModerationClassificationServiceTests
{
    private readonly LocalModerationClassificationService _service;

    public LocalModerationClassificationServiceTests()
    {
        var validator = new LocalDocumentFileValidator();
        _service = new LocalModerationClassificationService(validator);
    }

    [Fact]
    public async Task ClassifyAsync_WithValidContent_ReturnsAllowed()
    {
        using var content = new MemoryStream(new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x04, 0x00, 0x04, 0x80, 0x03
        });

        var result = await _service.ClassifyAsync(content, "image/jpeg");

        Assert.Equal(ModerationClassificationStatus.Allowed, result.Status);
    }

    [Fact]
    public async Task ClassifyAsync_WithInvalidContent_ReturnsProhibited()
    {
        using var content = new MemoryStream(new byte[]
        {
            0x4D, 0x5A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        });

        var result = await _service.ClassifyAsync(content, "application/octet-stream");

        Assert.Equal(ModerationClassificationStatus.Prohibited, result.Status);
    }

    [Fact]
    public async Task ClassifyAsync_WithEmptyContent_ReturnsUncertain()
    {
        using var content = new MemoryStream(Array.Empty<byte>());

        var result = await _service.ClassifyAsync(content, "image/jpeg");

        Assert.Equal(ModerationClassificationStatus.Uncertain, result.Status);
    }
}
