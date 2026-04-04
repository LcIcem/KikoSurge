using System;

/// <summary>
/// 加密工具类，用于存档防篡改和热更新 Hash 校验
/// </summary>
public static class EncryptUtil
{
    /// <summary>
    /// MD5 哈希
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string MD5(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        byte[] bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// SHA256 哈希（热更新 Hash 校验推荐用这个）
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string SHA256(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// 对文件计算 SHA256
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static string SHA256File(string filePath)
    {
        if (!System.IO.File.Exists(filePath)) return string.Empty;
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
        byte[] hash = sha.ComputeHash(fs);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// AESEncrypt：AES 对称加密
    /// </summary>
    /// <param name="plainText"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string AESEncrypt(string plainText, string key)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = System.Text.Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        aes.IV = System.Text.Encoding.UTF8.GetBytes(key.PadRight(16).Substring(0, 16));
        using var encryptor = aes.CreateEncryptor();
        byte[] plain = System.Text.Encoding.UTF8.GetBytes(plainText);
        byte[] encrypted = encryptor.TransformFinalBlock(plain, 0, plain.Length);
        return Convert.ToBase64String(encrypted);
    }
    
    /// <summary>
    /// AESDecrypt：AES 对称解密
    /// </summary>
    /// <param name="cipherText"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string AESDecrypt(string cipherText, string key) {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = System.Text.Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        aes.IV = System.Text.Encoding.UTF8.GetBytes(key.PadRight(16).Substring(0, 16));
        using var decryptor = aes.CreateDecryptor();
        byte[] cipher = Convert.FromBase64String(cipherText);
        byte[] decrypted = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return System.Text.Encoding.UTF8.GetString(decrypted);
    }
}