using CleanHome.Application.Marketplace;

namespace CleanHome.Infrastructure.Marketplace;

public sealed class LocalPrivateFileStore(string root) : IPrivateFileStore
{
    private readonly string directory = Path.GetFullPath(root);
    public async Task<string> WriteAsync(Stream source, CancellationToken ct)
    {
        Directory.CreateDirectory(directory);
        var key = Guid.NewGuid().ToString("N");
        try
        {
            await using var target = new FileStream(Path.Combine(directory, key), FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await source.CopyToAsync(target, ct);
            return key;
        }
        catch { Delete(key); throw; }
    }
    private string Resolve(string key)
    {
        if (!Guid.TryParseExact(key, "N", out _)) throw new InvalidOperationException("Clave de archivo inválida.");
        return Path.Combine(directory, key);
    }
    public Stream Read(string key) => new FileStream(Resolve(key), FileMode.Open, FileAccess.Read, FileShare.Read);
    public void Delete(string key) => File.Delete(Resolve(key));
}
