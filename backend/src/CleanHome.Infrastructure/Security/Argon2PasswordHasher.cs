using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CleanHome.Application.Abstractions.Identity;
using Konscious.Security.Cryptography;

namespace CleanHome.Infrastructure.Security;

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int MemoryKiB = 19456;
    private const int Iterations = 2;
    private const int Parallelism = 1;
    private const int SaltLength = 16;
    private const int HashLength = 32;

    private const string Version = "v=19";

    public string HashPassword(string password)
    {
        ValidatePassword(password);

        var salt = RandomNumberGenerator.GetBytes(SaltLength);

        var hash = DeriveKey(
            password,
            salt,
            MemoryKiB,
            Iterations,
            Parallelism);

        return string.Join(
            "$",
            "",
            "argon2id",
            Version,
            $"m={MemoryKiB},t={Iterations},p={Parallelism}",
            Convert.ToBase64String(salt).TrimEnd('='),
            Convert.ToBase64String(hash).TrimEnd('='));
    }

    public bool VerifyPassword(
        string password,
        string encodedHash)
    {
        if (string.IsNullOrEmpty(password) ||
            !TryParseHash(encodedHash, out var parsed))
        {
            return false;
        }

        var calculated = DeriveKey(
            password,
            parsed.Salt,
            parsed.MemoryKiB,
            parsed.Iterations,
            parsed.Parallelism);

        return CryptographicOperations.FixedTimeEquals(
            calculated,
            parsed.Hash);
    }

    public bool NeedsRehash(string encodedHash)
    {
        if (!TryParseHash(encodedHash, out var parsed))
            return true;

        return parsed.MemoryKiB != MemoryKiB ||
               parsed.Iterations != Iterations ||
               parsed.Parallelism != Parallelism ||
               parsed.Hash.Length != HashLength ||
               parsed.Salt.Length != SaltLength;
    }

    private static byte[] DeriveKey(
        string password,
        byte[] salt,
        int memoryKiB,
        int iterations,
        int parallelism)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        try
        {
            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = salt,
                DegreeOfParallelism = parallelism,
                Iterations = iterations,
                MemorySize = memoryKiB
            };

            return argon2.GetBytes(HashLength);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(
                "La contraseña es obligatoria.",
                nameof(password));
        }

        if (Encoding.UTF8.GetByteCount(password) > 1024)
        {
            throw new ArgumentException(
                "La contraseña excede el tamaño máximo permitido.",
                nameof(password));
        }
    }

    private static bool TryParseHash(
        string? encodedHash,
        out ParsedHash parsed)
    {
        parsed = default;

        if (string.IsNullOrWhiteSpace(encodedHash) ||
            encodedHash.Length > 512)
        {
            return false;
        }

        var parts = encodedHash.Split('$');

        if (parts.Length != 6 ||
            parts[0] != "" ||
            parts[1] != "argon2id" ||
            parts[2] != Version)
        {
            return false;
        }

        var parameters = parts[3].Split(',');

        if (parameters.Length != 3 ||
            !TryReadParameter(parameters[0], "m=", out int memory) ||
            !TryReadParameter(parameters[1], "t=", out int iterations) ||
            !TryReadParameter(parameters[2], "p=", out int parallelism))
        {
            return false;
        }

        // Límites defensivos para no permitir que un hash
        // manipulado provoque consumo excesivo de recursos.
        if (memory < MemoryKiB ||
            memory > 262144 ||
            iterations < 1 ||
            iterations > 10 ||
            parallelism < 1 ||
            parallelism > 4)
        {
            return false;
        }

        try
        {
            var salt = DecodeBase64(parts[4]);
            var hash = DecodeBase64(parts[5]);

            if (salt.Length < SaltLength ||
                salt.Length > 64 ||
                hash.Length != HashLength)
            {
                return false;
            }

            parsed = new ParsedHash(
                memory,
                iterations,
                parallelism,
                salt,
                hash);

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryReadParameter(
        string value,
        string prefix,
        out int result)
    {
        result = 0;

        return value.StartsWith(prefix, StringComparison.Ordinal) &&
               int.TryParse(
                   value.AsSpan(prefix.Length),
                   NumberStyles.None,
                   CultureInfo.InvariantCulture,
                   out result);
    }

    private static byte[] DecodeBase64(string value)
    {
        var padded = value.PadRight(
            (value.Length + 3) / 4 * 4,
            '=');

        return Convert.FromBase64String(padded);
    }

    private readonly record struct ParsedHash(
        int MemoryKiB,
        int Iterations,
        int Parallelism,
        byte[] Salt,
        byte[] Hash);
}
