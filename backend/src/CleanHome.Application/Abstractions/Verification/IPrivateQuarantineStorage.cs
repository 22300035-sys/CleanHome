namespace CleanHome.Application.Abstractions.Verification;

public sealed record QuarantinedFile(
    string StorageKey,
    long ByteLength,
    string Sha256);

public interface IPrivateQuarantineStorage
{
    Task<QuarantinedFile> SaveAsync(
        Stream content,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}
