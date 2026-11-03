using CleanHome.Application.Abstractions.Verification;
using CleanHome.Application.Features.Verification;
using CleanHome.Infrastructure.Validation;

namespace CleanHome.Infrastructure.Security;

public sealed class LocalModerationClassificationService : IModerationClassificationService
{
    private readonly IDocumentFileValidator _validator;

    public LocalModerationClassificationService(
        IDocumentFileValidator validator)
    {
        _validator = validator;
    }

    public async Task<ModerationClassificationResult> ClassifyAsync(
        Stream content,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        if (content is null)
            return new ModerationClassificationResult(
                ModerationClassificationStatus.Uncertain,
                "Contenido no disponible para moderacion.");

        using var buffer = new MemoryStream();

        await content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length == 0)
            return new ModerationClassificationResult(
                ModerationClassificationStatus.Uncertain,
                "Contenido vacio.");

        buffer.Position = 0;

        var validation = await _validator.ValidateAsync(buffer, cancellationToken);

        if (!validation.IsValid)
            return new ModerationClassificationResult(
                ModerationClassificationStatus.Prohibited,
                validation.Reason);

        return new ModerationClassificationResult(
            ModerationClassificationStatus.Allowed);
    }
}
