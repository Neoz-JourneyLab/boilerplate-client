using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class Crypto {
  public static string Hash(string txt) => Convert.ToBase64String(new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(txt)));

  public static string Encrypt(string info, string pass = "", int minLen = 16) {
    info = info.Length + "___" + info;
    while (info.Length < minLen) {
      info += Guid.NewGuid();
    }

    string passhash = pass != "" ? pass : PlayerPrefs.GetString("pass");

    byte[] infoByte = Encoding.UTF8.GetBytes(info);
    byte[] encryptedByte = new byte[infoByte.Length];
    byte[] passByte = Encoding.UTF8.GetBytes(passhash);
    for (int i = 0; i < infoByte.Length; i++) {
      encryptedByte[i] = (byte)(infoByte[i] ^ passByte[i % passByte.Length]);
    }

    return Convert.ToBase64String(encryptedByte);
  }

  internal static string Decrypt(string info, string pass = "") {
    string passhash = pass != "" ? pass : PlayerPrefs.GetString("pass");

    byte[] infoByte = Convert.FromBase64String(info);
    byte[] decryptedByte = new byte[infoByte.Length];
    byte[] passByte = Encoding.UTF8.GetBytes(passhash);
    for (int i = 0; i < infoByte.Length; i++) {
      decryptedByte[i] = (byte)(infoByte[i] ^ passByte[i % passByte.Length]);
    }

    string decrypt = Encoding.UTF8.GetString(decryptedByte);
    int len = int.Parse(decrypt.Split(new[] { "___" }, StringSplitOptions.RemoveEmptyEntries).First());
    return decrypt.Split(new[] { "___" }, StringSplitOptions.RemoveEmptyEntries).Last().Substring(0, len);
  }
  
  internal static string EncryptionRSA(string plainText, string publicKeyXml) {
    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
    rsa.FromXmlString(publicKeyXml);

    byte[] dataToEncrypt = Encoding.UTF8.GetBytes(plainText);
    byte[] encryptedData = rsa.Encrypt(dataToEncrypt, true);
    return Convert.ToBase64String(encryptedData);
  }

  internal static string DecryptionRSA(string encryptedData, string privateKeyXml) {
    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
    rsa.FromXmlString(privateKeyXml);

    byte[] dataToDecrypt = Convert.FromBase64String(encryptedData);
    byte[] decryptedData = rsa.Decrypt(dataToDecrypt, true);
    return Encoding.UTF8.GetString(decryptedData);
  }
}
