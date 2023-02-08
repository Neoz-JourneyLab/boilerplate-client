using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Crypto tools for encryption
/// </summary>
public static class Crypto {
	//cryptocraphic number generator
	private static readonly RandomNumberGenerator csp = RandomNumberGenerator.Create();

	/// <summary>
	/// create a 256 bit (32 bytes) hash
	/// </summary>
	/// <param name="data">byte array of the input</param>
	/// <returns>hashed array : byte[32]</returns>
	public static byte[] Hash(byte[] data) {
		var hashed = SHA256.Create().ComputeHash(data);
		return hashed;
	}

	/// <summary>
	/// Generate a random Hash from CSP
	/// </summary>
	/// <returns>random byte[32] array</returns>
	public static byte[] GenerateRandomHash() {
		var hash = new byte[32];
		csp.GetBytes(hash);
		hash = Hash(hash);
		return hash;
	}

	/// <summary>
	/// Works like a hash with a number of recursive call
	/// </summary>
	/// <param name="init">input base hash (byte[32)</param>
	/// <param name="salt">salt for randomization (byte[32)</param>
	/// <param name="clicks">number of iterations (min 1)</param>
	/// <returns>byte[32] of computed KDF</returns>
	public static byte[] KDF(byte[] init, byte[] salt, int clicks) {
		if (clicks == 0) clicks = 1;
		int keyLength = 256;

		var kdf = new Rfc2898DeriveBytes(init, salt, clicks);
		byte[] key = kdf.GetBytes(keyLength / 8);

		return key;
	}

	/// <summary>
	/// AES symetrical encryption
	/// </summary>
	/// <param name="plainText">string text</param>
	/// <param name="Key">byte[32] key</param>
	/// <param name="IV">byte[16] Initilization vector</param>
	/// <returns>cipher byte[n]</returns>
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

	/// <summary>
	/// AES symetrical desencryption
	/// </summary>
	/// <param name="cipherText">encrypted cipher byte[n]</param>
	/// <param name="Key">byte[32] key</param>
	/// <param name="IV">byte[16] Initilization vector</param>
	/// <returns>plain text (string)</returns>
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

	/// <summary>
	/// assymetrical RSA encryption
	/// </summary>
	/// <param name="plainText">string text</param>
	/// <param name="publicKeyXml">public XML key</param>
	/// <returns>cipher base 64 string</returns>
	internal static string EncryptionRSA(string plainText, string publicKeyXml) {
		RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
		rsa.FromXmlString(publicKeyXml);

		byte[] dataToEncrypt = Encoding.UTF8.GetBytes(plainText);
		byte[] encryptedData = rsa.Encrypt(dataToEncrypt, true);
		return Convert.ToBase64String(encryptedData);
	}

	/// <summary>
	/// assymetrical RSA decryption
	/// </summary>
	/// <param name="encryptedData">base 64 cipher</param>
	/// <param name="privateKeyXml">string XML private key</param>
	/// <returns>string plain text</returns>
	internal static string DecryptionRSA(string encryptedData, string privateKeyXml) {
		RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
		rsa.FromXmlString(privateKeyXml);

		byte[] dataToDecrypt = Convert.FromBase64String(encryptedData);
		byte[] decryptedData = rsa.Decrypt(dataToDecrypt, true);
		return Encoding.UTF8.GetString(decryptedData);
	}

	/// <summary>
	/// Simplify XML string for public RSA key by removing common fields markers
	/// </summary>
	/// <param name="xml">exported XML string</param>
	/// <returns>simplified XML string</returns>
	public static string Simplfy_XML_RSA(string xml) {
		return xml.Replace("<RSAKeyValue><Modulus>", "").Replace("</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>", "");
	}

	/// <summary>
	/// Regen a XML string by adding back XML tags
	/// </summary>
	/// <param name="xml">simplified XML</param>
	/// <returns>complete XML</returns>
	public static string Regen_XML_RSA(string xml) {
		return "<RSAKeyValue><Modulus>" + xml + "</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
	}

	//index if this list give the corresponding byte
	//i.e. by default the byte 128 correspond to the index 128 that is equal to value of 72
	static Dictionary<byte, byte> adfgvx = new Dictionary<byte, byte>();

	/// <summary>
	/// Get ADFGVX (hex 16 * 16) and generate default config if not specified.
	/// </summary>
	/// <returns>byte correspondance</returns>
	public static Dictionary<byte, byte> GetAdfGvx() {
		if (adfgvx.Count > 0) return adfgvx;

		string path = Application.streamingAssetsPath + "/" + User.nickname + "_ADFGVX.txt";
		if (!File.Exists(path)) {
			List<int> vals = Enumerable.Range(0, 256).OrderBy(x => (Math.Pow(x, 1.4f) * 9 + 7) % 256).ToList();
			for (int i = 0; i <= 255; i++) {
				//default formula
				int index = i == 0 ? 0 : (int)(Math.Pow(i, 1.4) * 3 + 9) % 256;
				adfgvx.Add((byte)i, (byte)vals[i]);
			}
			File.WriteAllText(path, JsonConvert.SerializeObject(adfgvx));
		} else {
			adfgvx = JsonConvert.DeserializeObject<Dictionary<byte, byte>>(File.ReadAllText(path));
		}
		return adfgvx;
	}

	/// <summary>
	/// get back byte[] from HEX following adfgvx chart
	/// </summary>
	/// <param name="text">HEX input</param>
	/// <returns>byte[n] array</returns>
	public static byte[] ToObfuscatedBytes(string text) {
		List<byte> bytes = new List<byte>();
		for (int i = 0; i < text.Length; i += 2) {
			string b_hex = text.Substring(i, 2);
			byte b = byte.Parse(b_hex, System.Globalization.NumberStyles.HexNumber);
			bytes.Add(GetAdfGvx()[b]); //add not the value but the index of the value
		}
		return bytes.ToArray();
	}

	/// <summary>
	/// generate HEX string from byte[n] array with adfgvx chart
	/// </summary>
	/// <param name="bytes">binary data in a byte[n] array</param>
	/// <returns>HEX string</returns>
	public static string FromObfuscatedByte(byte[] bytes) {
		string txt = "";
		foreach (var b in bytes) {
			txt += GetAdfGvx().First(x => x.Value == b).Key.ToString("X2"); //add not the value but the value of the index
		}
		return txt;
	}
}
