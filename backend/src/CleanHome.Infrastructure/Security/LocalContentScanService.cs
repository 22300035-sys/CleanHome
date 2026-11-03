using CleanHome.Application.Abstractions.Verification;

namespace CleanHome.Infrastructure.Security;

public sealed class LocalContentScanService : IContentScanService
{
    public async Task<ContentScanResult> ScanAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (content is null)
            return new ContentScanResult(
                ContentScanStatus.Pending,
                "Contenido no disponible para analisis.");

        using var buffer = new MemoryStream();

        await content.CopyToAsync(buffer, cancellationToken);

        var isEmpty = buffer.Length == 0;

        // TODO grupo 4.2: sustituir el analizador simulado por el motor
        // gratuito seleccionado con aislamiento y limites de recursos.
        var status = isEmpty
            ? ContentScanStatus.Pending
            : ContentScanStatus.Clean;

        return new ContentScanResult(
            status, isEmpty ? "Contenido vacio." : null);
    }
}
