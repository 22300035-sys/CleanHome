using System.Buffers;
using System.Text;
using CleanHome.Application.Abstractions.Verification;

namespace CleanHome.Infrastructure.Validation;

public sealed class LocalDocumentFileValidator : IDocumentFileValidator
{
    public const long MaxImageBytes = 5 * 1024 * 1024;
    public const long MaxPdfBytes = 10 * 1024 * 1024;
    public const long MaxImagePixels = 20 * 1000 * 1000;

    private const int BufferSize = 64 * 1024;

    public async Task<DocumentValidationResult> ValidateAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (content is null)
            return new DocumentValidationResult(
                false, "El contenido del archivo es obligatorio.");

        if (!content.CanRead)
            return new DocumentValidationResult(
                false, "El contenido del archivo no puede leerse.");

        await using var buffered = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        long totalBytes = 0;

        try
        {
            int read;

            while ((read = await content.ReadAsync(
                buffer.AsMemory(0, BufferSize),
                cancellationToken)) > 0)
            {
                totalBytes += read;

                if (totalBytes > MaxPdfBytes)
                    return new DocumentValidationResult(
                        false, "El archivo supera el tamaño permitido.", IsTooLarge: true);

                await buffered.WriteAsync(
                    buffer.AsMemory(0, read),
                    cancellationToken);
            }

            if (buffered.Length == 0)
                return new DocumentValidationResult(
                    false, "El archivo está vacío.");

            var contentBytes = buffered.ToArray();

            if (IsJpeg(contentBytes))
            {
                if (contentBytes.LongLength > MaxImageBytes)
                    return new DocumentValidationResult(
                        false, "El archivo supera el tamaño permitido.", IsTooLarge: true);

                if (!TryGetJpegDimensions(contentBytes, out var jpegWidth, out var jpegHeight) ||
                    (long)jpegWidth * jpegHeight > MaxImagePixels)
                    return new DocumentValidationResult(
                        false, "La imagen no cumple con los límites de dimensiones permitidos.");

                return new DocumentValidationResult(
                    true, "El archivo es válido.", "image/jpeg");
            }

            if (IsPng(contentBytes))
            {
                if (contentBytes.LongLength > MaxImageBytes)
                    return new DocumentValidationResult(
                        false, "El archivo supera el tamaño permitido.", IsTooLarge: true);

                if (!TryGetPngDimensions(contentBytes, out var pngWidth, out var pngHeight) ||
                    (long)pngWidth * pngHeight > MaxImagePixels)
                    return new DocumentValidationResult(
                        false, "La imagen no cumple con los límites de dimensiones permitidos.");

                return new DocumentValidationResult(
                    true, "El archivo es válido.", "image/png");
            }

            if (IsPdf(contentBytes))
            {
                if (contentBytes.LongLength > MaxPdfBytes)
                    return new DocumentValidationResult(
                        false, "El archivo supera el tamaño permitido.", IsTooLarge: true);

                if (ContainsForbiddenPdfContent(contentBytes))
                    return new DocumentValidationResult(
                        false, "El documento PDF contiene contenido no permitido.");

                return new DocumentValidationResult(
                    true, "El archivo es válido.", "application/pdf");
            }

            return new DocumentValidationResult(
                false, "El formato del archivo no está permitido.");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static bool TryGetJpegDimensions(
        byte[] bytes,
        out int width,
        out int height)
    {
        width = 0;
        height = 0;

        if (bytes.Length < 12)
            return false;

        var offset = 2;

        while (offset + 9 < bytes.Length)
        {
            if (bytes[offset] != 0xFF)
                return false;

            var marker = bytes[offset + 1];

            if (marker == 0xD8 || (marker >= 0xD0 && marker <= 0xD7) || marker == 0x01)
            {
                offset += 2;
                continue;
            }

            if (marker >= 0xC0 && marker <= 0xCF &&
                marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
            {
                if (offset + 10 > bytes.Length)
                    return false;

                height = (bytes[offset + 5] << 8) | bytes[offset + 6];
                width = (bytes[offset + 7] << 8) | bytes[offset + 8];

                return width > 0 && height > 0;
            }

            if (offset + 4 > bytes.Length)
                return false;

            var segmentLength = (bytes[offset + 2] << 8) | bytes[offset + 3];

            if (segmentLength < 2 || offset + 1 + segmentLength > bytes.Length)
                return false;

            offset += 1 + segmentLength;
        }

        return false;
    }

    private static bool TryGetPngDimensions(
        byte[] bytes,
        out int width,
        out int height)
    {
        width = 0;
        height = 0;

        if (bytes.Length < 24)
            return false;

        width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];

        return width > 0 && height > 0;
    }

    private static bool IsJpeg(byte[] bytes) =>
        bytes.Length >= 3 &&
        bytes[0] == 0xFF &&
        bytes[1] == 0xD8 &&
        bytes[2] == 0xFF;

    private static bool IsPng(byte[] bytes) =>
        bytes.Length >= 8 &&
        bytes[0] == 0x89 &&
        bytes[1] == 0x50 &&
        bytes[2] == 0x4E &&
        bytes[3] == 0x47 &&
        bytes[4] == 0x0D &&
        bytes[5] == 0x0A &&
        bytes[6] == 0x1A &&
        bytes[7] == 0x0A;

    private static bool ContainsForbiddenPdfContent(byte[] bytes)
    {
        var content = Encoding.Latin1.GetString(bytes);

        return content.Contains("/JavaScript", StringComparison.Ordinal) ||
               content.Contains("/OpenAction", StringComparison.Ordinal) ||
               content.Contains("/Launch", StringComparison.Ordinal);
    }

    private static bool IsPdf(byte[] bytes) =>
        bytes.Length >= 5 &&
        bytes[0] == 0x25 &&
        bytes[1] == 0x50 &&
        bytes[2] == 0x44 &&
        bytes[3] == 0x46 &&
        bytes[4] == 0x2D;
}
