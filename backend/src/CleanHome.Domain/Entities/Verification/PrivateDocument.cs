namespace CleanHome.Domain.Entities.Verification;

public sealed class PrivateDocument
{
    public Guid Id { get; private set; }
    public Guid OwnerUserId { get; private set; }

    // identity_front, identity_back, portrait, address_proof
    public string DocumentType { get; private set; } = "";

    // Solo se usa para comprobantes de domicilio.
    public Guid? CustomerLocationId { get; private set; }

    // Identificador interno generado por el almacenamiento seguro.
    public string StorageKey { get; private set; } = "";

    public string MimeType { get; private set; } = "";
    public long ByteLength { get; private set; }
    public string Sha256 { get; private set; } = "";

    // pending, clean, blocked
    public string SecurityStatus { get; private set; } = "pending";

    // pending, approved, rejected
    public string ModerationStatus { get; private set; } = "pending";

    // pending, approved, rejected
    public string ReviewStatus { get; private set; } = "pending";

    public DateTimeOffset UploadedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }

    private PrivateDocument() { }

    public PrivateDocument(
        Guid ownerUserId,
        string documentType,
        Guid? customerLocationId,
        string storageKey,
        string mimeType,
        long byteLength,
        string sha256)
    {
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("Propietario inválido.");

        if (documentType is not (
            "identity_front" or "identity_back" or
            "portrait" or "address_proof"))
        {
            throw new ArgumentException("Tipo de documento inválido.");
        }

        if ((documentType == "address_proof") !=
            customerLocationId.HasValue)
        {
            throw new ArgumentException(
                "El comprobante requiere una ubicación.");
        }

        if (customerLocationId == Guid.Empty)
            throw new ArgumentException("Ubicación inválida.");

        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey.Length > 300)
        {
            throw new ArgumentException(
                "Referencia privada inválida.");
        }

        if (mimeType is not (
            "image/jpeg" or "image/png" or "application/pdf"))
        {
            throw new ArgumentException("Formato no permitido.");
        }

        if (byteLength <= 0 || byteLength > 10_485_760)
            throw new ArgumentException("Tamaño no permitido.");

        if (string.IsNullOrWhiteSpace(sha256) ||
            sha256.Length != 64 ||
            !sha256.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("Hash inválido.");
        }

        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        DocumentType = documentType;
        CustomerLocationId = customerLocationId;
        StorageKey = storageKey.Trim();
        MimeType = mimeType;
        ByteLength = byteLength;
        Sha256 = sha256.ToLowerInvariant();

        SecurityStatus = "pending";
        ModerationStatus = "pending";
        ReviewStatus = "pending";
        UploadedAt = DateTimeOffset.UtcNow;
    }

    public void SetSecurityStatus(string status)
    {
        if (status is not ("pending" or "clean" or "blocked"))
            throw new ArgumentException("Estado de seguridad inválido.");

        SecurityStatus = status;

        if (status != "clean")
            ClearApproval();
    }

    public void SetModerationStatus(string status)
    {
        if (status is not ("pending" or "approved" or "rejected"))
            throw new ArgumentException("Estado de moderación inválido.");

        ModerationStatus = status;

        if (status != "approved")
            ClearApproval();
    }

    public void SetReviewStatus(string status, Guid reviewerUserId)
    {
        if (reviewerUserId == Guid.Empty)
            throw new ArgumentException("Revisor inválido.");

        if (status is not ("pending" or "approved" or "rejected"))
            throw new ArgumentException("Estado de revisión inválido.");

        if (status == "approved" &&
            (SecurityStatus != "clean" ||
             ModerationStatus != "approved"))
        {
            throw new InvalidOperationException(
                "El archivo debe superar seguridad y moderación.");
        }

        ReviewStatus = status;
        ReviewedByUserId = reviewerUserId;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public bool IsApproved =>
        SecurityStatus == "clean" &&
        ModerationStatus == "approved" &&
        ReviewStatus == "approved";

    private void ClearApproval()
    {
        ReviewStatus = "pending";
        ReviewedAt = null;
        ReviewedByUserId = null;
    }
}
