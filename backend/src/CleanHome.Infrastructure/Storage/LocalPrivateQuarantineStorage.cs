using System.Buffers;
using System.Security.Cryptography;
using CleanHome.Application.Abstractions.Verification;
using Microsoft.Extensions.Configuration;

namespace CleanHome.Infrastructure.Storage;

public sealed class LocalPrivateQuarantineStorage
    : IPrivateQuarantineStorage
{
    private const long MaxFileBytes = 10 * 1024 * 1024;

    private readonly string _directory;

    public LocalPrivateQuarantineStorage(
        IConfiguration configuration)
    {
        var environmentName =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";

        if (environmentName is not ("Development" or "Testing"))
        {
            throw new InvalidOperationException(
                "El almacenamiento local solo puede utilizarse " +
                "en desarrollo y pruebas.");
        }

        var configuredDirectory =
            configuration["PrivateStorage:LocalDirectory"];

        var baseDirectory = string.IsNullOrWhiteSpace(
            configuredDirectory)
            ? Path.Combine(
                Path.GetTempPath(),
                "cleanhome-private-development")
            : Path.GetFullPath(configuredDirectory);

        _directory = Path.Combine(
            baseDirectory,
            "quarantine");

        Directory.CreateDirectory(_directory);

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                baseDirectory,
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.UserExecute);

            File.SetUnixFileMode(
                _directory,
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.UserExecute);
        }
    }

    public async Task<QuarantinedFile> SaveAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanRead)
        {
            throw new ArgumentException(
                "El contenido no puede leerse.",
                nameof(content));
        }

        var storageKey = Guid.NewGuid().ToString("N");
        var destination = Path.Combine(_directory, storageKey);

        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        long total = 0;
        var completed = false;

        try
        {
            await using var output = new FileStream(
                destination,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                options: FileOptions.Asynchronous);

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(
                    destination,
                    UnixFileMode.UserRead |
                    UnixFileMode.UserWrite);
            }

            using var hash =
                IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            while (true)
            {
                var read = await content.ReadAsync(
                    buffer.AsMemory(0, buffer.Length),
                    cancellationToken);

                if (read == 0)
                    break;

                if (total + read > MaxFileBytes)
                {
                    throw new InvalidDataException(
                        "El archivo supera el límite de 10 MB.");
                }

                total += read;
                hash.AppendData(buffer, 0, read);

                await output.WriteAsync(
                    buffer.AsMemory(0, read),
                    cancellationToken);
            }

            if (total == 0)
            {
                throw new InvalidDataException(
                    "No se permiten archivos vacíos.");
            }

            await output.FlushAsync(cancellationToken);

            var sha256 = Convert.ToHexString(
                hash.GetHashAndReset()).ToLowerInvariant();

            completed = true;

            return new QuarantinedFile(
                storageKey,
                total,
                sha256);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
            ArrayPool<byte>.Shared.Return(buffer);

            if (!completed && File.Exists(destination))
                File.Delete(destination);
        }
    }
}
