using System.Security.Cryptography;
using System.Text;

namespace CleanHome.Infrastructure.Payments;

public static class WebhookSignatures
{
    public static bool Verify(string secret, string payload, IEnumerable<string> signatures)
    {
        if (string.IsNullOrEmpty(secret)) return false;
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        foreach (var signature in signatures)
        {
            try { if (CryptographicOperations.FixedTimeEquals(expected, Convert.FromHexString(signature))) return true; }
            catch (FormatException) { }
        }
        return false;
    }
    public static string[] Values(string header, string key) => header.Split(',').Select(x => x.Trim().Split('=', 2)).Where(x => x.Length == 2 && x[0] == key).Select(x => x[1]).ToArray();
    public static bool IsRecent(string timestamp, bool milliseconds = false)
    {
        if (!long.TryParse(timestamp, out var value)) return false;
        try
        {
            var date = milliseconds ? DateTimeOffset.FromUnixTimeMilliseconds(value) : DateTimeOffset.FromUnixTimeSeconds(value);
            return Math.Abs((DateTimeOffset.UtcNow - date).TotalMinutes) <= 5;
        }
        catch (ArgumentOutOfRangeException) { return false; }
    }
}
