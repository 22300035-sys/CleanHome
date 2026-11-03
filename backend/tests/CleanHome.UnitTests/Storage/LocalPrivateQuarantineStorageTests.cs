using System.Security.Cryptography;
using CleanHome.Application.Abstractions.Verification;
using CleanHome.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;

namespace CleanHome.UnitTests.Storage;

public sealed class LocalPrivateQuarantineStorageTests : IDisposable
{
    private const string QuarantineFolderName = "quarantine";

    private readonly LocalPrivateQuarantineStorage _storage;
    private readonly string _quarantineDirectory;
    private readonly string? _previousEnvironment;

    public LocalPrivateQuarantineStorageTests()
    {
        _previousEnvironment = Environment.GetEnvironmentVariable(
            "ASPNETCORE_ENVIRONMENT");

        Environment.SetEnvironmentVariable(
            "ASPNETCORE_ENVIRONMENT", "Testing");

        var baseDirectory = Path.Combine(
            Path.GetTempPath(),
            $"cleanhome-private-tests-{Guid.NewGuid():N}");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PrivateStorage:LocalDirectory"] = baseDirectory
            })
            .Build();

        _storage = new LocalPrivateQuarantineStorage(configuration);
        _quarantineDirectory = Path.Combine(
            baseDirectory, QuarantineFolderName);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(
            "ASPNETCORE_ENVIRONMENT", _previousEnvironment);

        if (Directory.Exists(_quarantineDirectory))
            Directory.Delete(_quarantineDirectory, recursive: true);

        if (Directory.Exists(Path.GetDirectoryName(_quarantineDirectory)!))
            Directory.Delete(
                Path.GetDirectoryName(_quarantineDirectory)!,
                recursive: true);
    }

    [Fact]
    public async Task SaveAsync_WithValidContent_ReturnsStorageKeyAndSha256()
    {
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

        var result = await _storage.SaveAsync(
            new MemoryStream(content));

        Assert.False(string.IsNullOrWhiteSpace(result.StorageKey));
        Assert.Equal(content.Length, result.ByteLength);

        var expectedSha256 = Convert.ToHexString(
            SHA256.HashData(content)).ToLowerInvariant();

        Assert.Equal(expectedSha256, result.Sha256);
    }

    [Fact]
    public async Task SaveAsync_WithEmptyContent_ThrowsWithoutPersistingFile()
    {
        var before = GetPersistedFileCount();

        await Assert.ThrowsAsync<InvalidDataException>(
            () => _storage.SaveAsync(new MemoryStream(Array.Empty<byte>())));

        Assert.Equal(before, GetPersistedFileCount());
    }

    [Fact]
    public async Task SaveAsync_WithContentOverLimit_ThrowsWithoutPersistingFile()
    {
        var before = GetPersistedFileCount();

        var oversized = new byte[10 * 1024 * 1024 + 1];

        await Assert.ThrowsAsync<InvalidDataException>(
            () => _storage.SaveAsync(new MemoryStream(oversized)));

        Assert.Equal(before, GetPersistedFileCount());
    }

    [Fact]
    public async Task DeleteAsync_WithExistingStorageKey_RemovesFileAndReturnsTrue()
    {
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        var result = await _storage.SaveAsync(new MemoryStream(content));
        var path = Path.Combine(_quarantineDirectory, result.StorageKey);

        Assert.True(File.Exists(path));

        var deleted = await _storage.DeleteAsync(result.StorageKey);

        Assert.True(deleted);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownStorageKey_ReturnsFalse()
    {
        var deleted = await _storage.DeleteAsync(Guid.NewGuid().ToString("N"));

        Assert.False(deleted);
    }

    private int GetPersistedFileCount() =>
        Directory.Exists(_quarantineDirectory)
            ? Directory.GetFiles(_quarantineDirectory).Length
            : 0;
}
