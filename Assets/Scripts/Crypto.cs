using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class Crypto {
	private static readonly RandomNumberGenerator csp = RandomNumberGenerator.Create();

	public static byte[] Hash(byte[] data) {
		var hashed = SHA256.Create().ComputeHash(data);
		return hashed;
	}
	public static byte[] GenerateRandomHash() {
		var hash = new byte[32];
		csp.GetBytes(hash);
		hash = Hash(hash);
		return hash;
	}
	public static byte[] KDF(byte[] init, byte[] salt, int clicks) {
		if(clicks == 0) clicks = 1;
		int keyLength = 256;

		var kdf = new Rfc2898DeriveBytes(init, salt, clicks);
		byte[] key = kdf.GetBytes(keyLength / 8);

		return key;
	}

	public static byte[] EncryptAES(string plainText, byte[] Key, byte[] IV) {
		byte[] encrypted;
		if (IV.Length != 16) {
			IV = IV.Skip(IV.Length - 16).ToArray();
		}
		// Create a new AesManaged.
		using (Aes aes = Aes.Create()) {
			// Create encryptor
			ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);
			// Create MemoryStream
			using MemoryStream ms = new MemoryStream();
			// Create crypto stream using the CryptoStream class. This class is the key to encryption
			// and encrypts and decrypts data from any given stream. In this case, we will pass a memory stream
			// to encrypt
			using CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
			// Create StreamWriter and write data to a stream
			using (StreamWriter sw = new StreamWriter(cs))
				sw.Write(plainText);
			encrypted = ms.ToArray();
		}
		// Return encrypted data
		return encrypted;
	}
	public static string DecryptAES(byte[] cipherText, byte[] Key, byte[] IV) {
		string plaintext = "cannot decrypt";
			if (IV.Length == 32) {
				IV = IV.Skip(16).ToArray();
			}
			// Create AesManaged
			using (Aes aes = Aes.Create()) {
				// Create a decryptor
				ICryptoTransform decryptor = aes.CreateDecryptor(Key, IV);
				// Create the streams used for decryption.
				using MemoryStream ms = new MemoryStream(cipherText);
				// Create crypto stream
				using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
				// Read crypto stream
				using StreamReader reader = new StreamReader(cs);
				plaintext = reader.ReadToEnd();
			}
		return plaintext;
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

	public static string Simplfy_XML_RSA(string xml) {
		return xml.Replace("<RSAKeyValue><Modulus>", "").Replace("</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>", "");
	}

	public static string Regen_XML_RSA(string xml) {
		return "<RSAKeyValue><Modulus>" + xml + "</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
	}
}
