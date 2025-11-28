using System.Security.Cryptography;

namespace Common.Utils;

/// <summary>
///     密码哈希服务
///     使用PBKDF2算法加密密码
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16; // 128位
    private const int KeySize = 32; // 256位
    private const int Iterations = 10000;

    /// <summary>
    ///     哈希密码
    /// </summary>
    public static string Hash(string password)
    {
        using var algorithm = new Rfc2898DeriveBytes(
            password,
            SaltSize,
            Iterations,
            HashAlgorithmName.SHA256);

        var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
        var salt = Convert.ToBase64String(algorithm.Salt);

        return $"{Iterations}.{salt}.{key}";
    }

    /// <summary>
    ///     验证密码
    /// </summary>
    public static bool Verify(string hash, string password)
    {
        var parts = hash.Split('.', 3);

        if (parts.Length != 3) return false;

        var iterations = Convert.ToInt32(parts[0]);
        var salt = Convert.FromBase64String(parts[1]);
        var key = Convert.FromBase64String(parts[2]);

        using var algorithm = new Rfc2898DeriveBytes(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256);

        var keyToCheck = algorithm.GetBytes(KeySize);

        return CryptographicOperations.FixedTimeEquals(keyToCheck, key);
    }
}