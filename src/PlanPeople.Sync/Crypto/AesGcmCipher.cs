using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PlanPeople.Sync.Crypto;

public static class AesGcmCipher
{
    private static readonly byte[] Magic = { 0x53, 0x50, 0x30, 0x31 }; // "SP01"
    private const byte Version = 0x01;
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static byte[] Encrypt(string plainText, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        
        // 派生密钥
        var key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, 100000, HashAlgorithmName.SHA256, 32);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherText = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherText, tag);

        // 拼接文件头：[Magic 4B][Version 1B][Salt 16B][Nonce 12B]
        var header = new byte[Magic.Length + 1 + SaltSize + NonceSize];
        Buffer.BlockCopy(Magic, 0, header, 0, Magic.Length);
        header[4] = Version;
        Buffer.BlockCopy(salt, 0, header, 5, SaltSize);
        Buffer.BlockCopy(nonce, 0, header, 5 + SaltSize, NonceSize);

        // 拼接整体：[Header] + [Ciphertext] + [Tag]
        var result = new byte[header.Length + cipherText.Length + tag.Length];
        Buffer.BlockCopy(header, 0, result, 0, header.Length);
        Buffer.BlockCopy(cipherText, 0, result, header.Length, cipherText.Length);
        Buffer.BlockCopy(tag, 0, result, header.Length + cipherText.Length, tag.Length);

        return result;
    }

    public static string Decrypt(byte[] encryptedData, string password)
    {
        if (encryptedData.Length < Magic.Length + 1 + SaltSize + NonceSize + TagSize)
            throw new CryptographicException("数据长度无效。");

        if (encryptedData[0] != Magic[0] || encryptedData[1] != Magic[1] || 
            encryptedData[2] != Magic[2] || encryptedData[3] != Magic[3])
            throw new CryptographicException("无效的文件格式（Magic 不匹配）。");
            
        if (encryptedData[4] != Version)
            throw new CryptographicException("不支持的文件版本。");

        var salt = new byte[SaltSize];
        var nonce = new byte[NonceSize];
        Buffer.BlockCopy(encryptedData, 5, salt, 0, SaltSize);
        Buffer.BlockCopy(encryptedData, 5 + SaltSize, nonce, 0, NonceSize);

        var headerLength = 5 + SaltSize + NonceSize;
        var cipherTextLength = encryptedData.Length - headerLength - TagSize;
        
        var cipherText = new byte[cipherTextLength];
        var tag = new byte[TagSize];
        Buffer.BlockCopy(encryptedData, headerLength, cipherText, 0, cipherTextLength);
        Buffer.BlockCopy(encryptedData, headerLength + cipherTextLength, tag, 0, TagSize);

        var key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, 100000, HashAlgorithmName.SHA256, 32);

        var plainBytes = new byte[cipherTextLength];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipherText, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}