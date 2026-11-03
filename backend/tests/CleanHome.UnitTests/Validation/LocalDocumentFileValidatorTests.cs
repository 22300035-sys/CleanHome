using CleanHome.Infrastructure.Validation;

namespace CleanHome.UnitTests.Validation;

public sealed class LocalDocumentFileValidatorTests
{
    private readonly LocalDocumentFileValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidJpegContent_ReturnsValid()
    {
        var content = CreateStream(new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x04, 0x00, 0x04, 0x80, 0x03
        });

        var result = await _validator.ValidateAsync(content);

        Assert.True(result.IsValid);
        Assert.Equal("image/jpeg", result.MimeType);
    }

    [Fact]
    public async Task ValidateAsync_WithValidPngContent_ReturnsValid()
    {
        var content = CreateStream(new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        });

        var result = await _validator.ValidateAsync(content);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithValidPdfContent_ReturnsValid()
    {
        var content = CreateStream(new byte[]
        {
            0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34,
            0x0A, 0x25, 0x50, 0x44, 0x46, 0x2D, 0x32, 0x36, 0x0A
        });

        var result = await _validator.ValidateAsync(content);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithDisguisedExecutable_ReturnsInvalid()
    {
        // Firmas de ejecutables (MZ/PE) con extensión simulada de imagen.
        var content = CreateStream(new byte[]
        {
            0x4D, 0x5A, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00
        });

        var result = await _validator.ValidateAsync(content);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithSvgContent_ReturnsInvalid()
    {
        var content = CreateStream(
            System.Text.Encoding.UTF8.GetBytes(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><svg/>"));

        var result = await _validator.ValidateAsync(content);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyContent_ReturnsInvalid()
    {
        var result = await _validator.ValidateAsync(
            CreateStream(Array.Empty<byte>()));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithPdfOverMaxBytes_ReturnsInvalid()
    {
        var content = CreateStream(CreateOversizedPdfBytes());

        var result = await _validator.ValidateAsync(content);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithImageOverMaxBytes_ReturnsInvalid()
    {
        var content = CreateStream(CreateOversizedPngBytes());

        var result = await _validator.ValidateAsync(content);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithJpegExceedingPixelLimit_ReturnsInvalid()
    {
        var content = CreateStream(new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x13, 0x88, 0x13, 0x88, 0x03
        });

        var result = await _validator.ValidateAsync(content);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithPngExceedingPixelLimit_ReturnsInvalid()
    {
        var content = CreateStream(new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x13, 0x88, 0x00, 0x00, 0x13, 0x88,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        });

        var result = await _validator.ValidateAsync(content);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithCorruptJpegContent_ReturnsInvalid()
    {
        var content = CreateStream(new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x00
        });

        var result = await _validator.ValidateAsync(content);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithCleanPdfContent_ReturnsValid()
    {
        var content = CreateStream(new byte[]
        {
            0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34,
            0x0A, 0x25, 0x50, 0x44, 0x46, 0x2D, 0x32, 0x36, 0x0A
        });

        var result = await _validator.ValidateAsync(content);

        Assert.True(result.IsValid);
        Assert.Equal("application/pdf", result.MimeType);
    }

    [Fact]
    public async Task ValidateAsync_WithPdfContainingActiveContent_ReturnsInvalid()
    {
        var content = CreateStream(System.Text.Encoding.Latin1.GetBytes(
            "%PDF-1.4\n1 0 obj << /Type /Catalog /Pages 2 0 R /JavaScript 3 0 R >> endobj"));

        var result = await _validator.ValidateAsync(content);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithCorruptPdfContent_ReturnsInvalid()
    {
        var content = CreateStream(new byte[]
        {
            0x25, 0x51, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34
        });

        var result = await _validator.ValidateAsync(content);

        Assert.False(result.IsValid);
    }

    private static Stream CreateStream(byte[] bytes) =>
        new MemoryStream(bytes);

    private static byte[] CreateOversizedPdfBytes() =>
        CreateOversizedBytes(LocalDocumentFileValidator.MaxPdfBytes, isPdf: true);

    private static byte[] CreateOversizedPngBytes() =>
        CreateOversizedBytes(LocalDocumentFileValidator.MaxImageBytes, isPng: true);

    private static byte[] CreateOversizedBytes(
        long maxBytes,
        bool isPdf = false,
        bool isPng = false)
    {
        var bytes = new byte[maxBytes + 1];

        if (isPdf)
        {
            bytes[0] = 0x25;
            bytes[1] = 0x50;
            bytes[2] = 0x44;
            bytes[3] = 0x46;
            bytes[4] = 0x2D;
        }
        else if (isPng)
        {
            bytes[0] = 0x89;
            bytes[1] = 0x50;
            bytes[2] = 0x4E;
            bytes[3] = 0x47;
            bytes[4] = 0x0D;
            bytes[5] = 0x0A;
            bytes[6] = 0x1A;
            bytes[7] = 0x0A;
        }

        return bytes;
    }
}
