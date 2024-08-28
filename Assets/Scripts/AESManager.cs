using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

public class AESManager : MonoBehaviour {
  //déchiffre un tableau d'octet en autre tableau d'octet avec une clé AES et un IV (vecteur d'initialisation)
  public static string DecryptAES(byte[] cipherText, byte[] key, byte[] iv) {
    using Aes aesAlg = Aes.Create();
    aesAlg.Key = key;
    aesAlg.IV = iv;
    aesAlg.Mode = CipherMode.CBC;
    aesAlg.Padding = PaddingMode.PKCS7;

    using MemoryStream msDecrypt = new MemoryStream(cipherText);
    using ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
    using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
    using StreamReader srDecrypt = new StreamReader(csDecrypt);
    return srDecrypt.ReadToEnd();
  }

  //chiffre un tableau d'octet en autre tableau d'octet avec une clé AES et un IV (vecteur d'initialisation)
  public static byte[] EncryptAES(byte[] buffer, byte[] key, byte[] iv) {
    using Aes aesAlg = Aes.Create();
    aesAlg.Key = key;
    aesAlg.IV = iv;
    aesAlg.Mode = CipherMode.CBC;
    aesAlg.Padding = PaddingMode.PKCS7;

    using MemoryStream msEncrypt = new MemoryStream();
    using ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
    using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
    csEncrypt.Write(buffer, 0, buffer.Length);
    csEncrypt.FlushFinalBlock();
    return msEncrypt.ToArray();
  }

  //génère un tableau d'octet aléatoires d'une longueur définie
  public static byte[] GenerateRandomBytes(int length) {
    using var rng = new RNGCryptoServiceProvider();
    byte[] randomBytes = new byte[length];
    rng.GetBytes(randomBytes);
    return randomBytes;
  }
}
