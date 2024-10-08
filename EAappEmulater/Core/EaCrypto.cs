﻿namespace EAappEmulater.Core;

public static class EaCrypto
{
    private const int _prime_10000th = 104729;
    private const int _prime_20000th = 224737;
    private const int _prime_30000th = 350377;

    /// <summary>
    /// Get RTP handshake code
    /// </summary>
    public static string GetRTPHandshakeCode()
    {
        var dateTime = DateTime.UtcNow;

        var year = (uint)dateTime.Year;
        var month = (uint)dateTime.Month;
        var day = (uint)dateTime.Day;

        var temp_value = (_prime_10000th * year) ^ (month * _prime_20000th) ^ (day * _prime_30000th);
        var hashed_timestamp = temp_value ^ (temp_value << 16) ^ (temp_value >> 16);

        return hashed_timestamp.ToString();
    }

    /// <summary>
    /// Get special byte array through string
    /// </summary>
    public static byte[] GetByteArray(string data)
    {
        data = data.ToLower();

        var memoryStream = new MemoryStream();
        var stringBuilder = new StringBuilder();

        var source = "0123456789abcdef";

        foreach (char c in data)
        {
            if (!source.Contains(c))
                continue;

            stringBuilder.Append(c);
        }

        byte[] result;
        if (stringBuilder.Length % 2 != 0)
        {
            result = Array.Empty<byte>();
        }
        else
        {
            data = stringBuilder.ToString();
            for (int i = 0; i < stringBuilder.Length / 2; i++)
            {
                memoryStream.WriteByte(Convert.ToByte(data.Substring(i * 2, 2), 16));
            }
            result = memoryStream.ToArray();
        }

        return result;
    }

    /// <summary>
    /// Convert byte array to hexadecimal string
    /// </summary>
    public static string ByteArrayToHex(byte[] bytes)
    {
        var strBuilder = new StringBuilder();

        foreach (byte b in bytes)
        {
            strBuilder.Append(b.ToString("x2"));
        }

        return strBuilder.ToString();
    }

    /// <summary>
    /// Obtain the ASE encryption object through the secret key
    /// </summary>
    public static Aes GetAesByKey(byte[] key)
    {
        // You cannot use using here, otherwise the object will be released early
        var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;

        return aes;
    }

    /// <summary>
    /// Decrypt
    /// </summary>
    public static string Decrypt(byte[] bytes)
    {
        using var rDel = Aes.Create("AesManaged");
        rDel.IV = new byte[16];
        rDel.Key = new byte[] { 65, 50, 114, 45, 208, 130, 239, 176, 220, 100, 87, 197, 118, 104, 202, 9 };
        rDel.Mode = CipherMode.CBC;
        rDel.Padding = PaddingMode.None;

        var cryptoTransform = rDel.CreateDecryptor();
        var decrypted = cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length);

        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>
    /// Decrypt
    /// </summary>
    public static string Decrypt(Aes aes, byte[] bytes)
    {
        try
        {
            var iCryptoTransform = aes.CreateDecryptor();
            var memoryStream = new MemoryStream();

            using var cryptoStream = new CryptoStream(memoryStream, iCryptoTransform, CryptoStreamMode.Write);
            cryptoStream.Write(bytes, 0, bytes.Length);
            cryptoStream.FlushFinalBlock();

            return Encoding.ASCII.GetString(memoryStream.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Encryption
    /// </summary>
    public static string Encrypt(Aes aes, byte[] bytes)
    {
        try
        {
            var iCryptoTransform = aes.CreateEncryptor();
            var memoryStream = new MemoryStream();

            using var cryptoStream = new CryptoStream(memoryStream, iCryptoTransform, CryptoStreamMode.Write);
            cryptoStream.Write(bytes, 0, bytes.Length);
            cryptoStream.FlushFinalBlock();

            return ByteArrayToHex(memoryStream.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Check Challenge response
    /// </summary>
    public static bool CheckChallengeResponse(string response, string key)
    {
        try
        {
            var aes = GetAesByKey(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
            var bytes = GetByteArray(response);

            return Decrypt(aes, bytes) == key;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Handle Challenge response
    /// </summary>
    public static string MakeChallengeResponse(string key)
    {
        var aes = GetAesByKey(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
        var bytes = Encoding.ASCII.GetBytes(key);

        return Encrypt(aes, bytes);
    }

    /// <summary>
    /// Get LSX key
    /// </summary>
    public static byte[] GetLSXKey(ushort seed)
    {
        var crandom = new CRandom();
        crandom.Seed(7u);
        crandom.Seed((uint)(crandom.Rand() + seed));

        var bytes = new byte[16];
        for (int i = 0; i < 16; i++)
        {
            bytes[i] = (byte)crandom.Rand();
        }

        return bytes;
    }

    /// <summary>
    /// BF4 LSX decryption
    /// </summary>
    public static string LSXDecryptBF4(string data, ushort seed)
    {
        if (string.IsNullOrWhiteSpace(data))
            return string.Empty;

        var key = GetLSXKey(seed);
        var aes = GetAesByKey(key);

        var bytes = GetByteArray(data);

        return Decrypt(aes, bytes);
    }

    /// <summary>
    /// BF4 LSX encryption
    /// </summary>
    public static string LSXEncryptBF4(string data, ushort seed)
    {
        if (string.IsNullOrWhiteSpace(data))
            return string.Empty;

        var key = GetLSXKey(seed);
        var aes = GetAesByKey(key);

        var bytes = Encoding.UTF8.GetBytes(data);

        return Encrypt(aes, bytes);
    }

    /// <summary>
    /// BFH LSX decryption
    /// </summary>
    public static string LSXDecryptBFH(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return string.Empty;

        var aes = GetAesByKey(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
        var bytes = GetByteArray(data);

        return Decrypt(aes, bytes);
    }

    /// <summary>
    /// BFH LSX encryption
    /// </summary>
    public static string LSXEncryptBFH(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return string.Empty;

        var aes = GetAesByKey(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
        var bytes = Encoding.ASCII.GetBytes(data);

        return Encrypt(aes, bytes);
    }
}
