using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// all user-relative statics infos are stored here
/// </summary>
public static class User {
	public static string nickname;
	public static string id;
	public static byte[] pass_kdf;
	public static byte[] pass_IV;
	public static Dictionary<string, UserInfo> users_infos = new Dictionary<string, UserInfo>();
	public static Dictionary<string, List<Message>> conversations = new Dictionary<string, List<Message>>();
	public static Dictionary<string, string> root_memory = new Dictionary<string, string>();
	static string default_private_key;

	public static Dictionary<string, DateTime> lastSaves = new Dictionary<string, DateTime>();
	static Dictionary<string, string> lastHashSave = new Dictionary<string, string>();

	/// <summary>
	/// Read or create the RSA private key linked to that user, used to be firstly contacted by foreign users.
	/// store it locally, encrypted with user main password
	/// </summary>
	public static void InitPrivateKey() {
		string default_rsa_key_path = Application.streamingAssetsPath + "/" + nickname + "_default_private_key.txt";
		if (File.Exists(default_rsa_key_path)) {
			string pk = File.ReadAllText(default_rsa_key_path);
			default_private_key = Crypto.DecryptAES(Convert.FromBase64String(pk), pass_kdf, pass_IV);
		} else {
			default_private_key = new RSACryptoServiceProvider().ToXmlString(true);
			string pk = Convert.ToBase64String(Crypto.EncryptAES(default_private_key, pass_kdf, pass_IV));
			File.WriteAllText(default_rsa_key_path, pk);
		}
	}

	/// <summary>
	/// get the default RSA public key
	/// </summary>
	public static string GetDefaultPublicKey() {
		var tmp = new RSACryptoServiceProvider();
		tmp.FromXmlString(default_private_key);
		return Crypto.Simplfy_XML_RSA(tmp.ToXmlString(false));
	}

	/// <summary>
	/// get the default RSA __PRIVATE__ key
	/// </summary>
	public static string WARNING___GetDefaultPrivateKey() {
		var tmp = new RSACryptoServiceProvider();
		tmp.FromXmlString(default_private_key);
		return tmp.ToXmlString(true);
	}

	/// <summary>
	/// Save all user-related data
	/// </summary>
	public static void SaveData(bool force = false) {
		if (force) {
			lastSaves.Clear();
		}
		Save("user_infos.txt", users_infos);
		Save("messages_roots.txt", root_memory);
	}

	/// <summary>
	/// save an object to a specified filename, json formatting, encrypted with user main password
	/// </summary>
	public static void Save(string filename, System.Object obj) {
		string json = JsonConvert.SerializeObject((object)obj);
		string encrypted = Convert.ToBase64String(Crypto.EncryptAES(json, pass_kdf, pass_IV));
		var hash = Convert.ToBase64String(Crypto.Hash(Encoding.UTF8.GetBytes(json)));
		if (!lastHashSave.ContainsKey(filename)) lastHashSave.Add(filename, hash);
		else if (lastHashSave[filename] == hash) {
			//Debug.Log("Hash is same for " + filename + " no saving.");
			return;
		} else if (lastSaves.ContainsKey(filename) && (DateTime.UtcNow - lastSaves[filename]).TotalSeconds < 5) {
			//Debug.Log("File " + filename + " saved " + (DateTime.UtcNow - lastSaves[filename]).TotalSeconds + " ago");
			return;
		}

		lastHashSave[filename] = hash;
		lastSaves[filename] = DateTime.UtcNow;

		File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_" + filename, encrypted);
		//File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_CLEAR_" + filename, json);
	}

	/// <summary>
	/// Load data from disk, decrypted with user main password
	/// </summary>
	public static void LoadData() {
		string user_info_path = Application.streamingAssetsPath + "/" + nickname + "_user_infos.txt";
		if (File.Exists(user_info_path)) {
			try {
				string encrypted = File.ReadAllText(user_info_path);
				string json = Crypto.DecryptAES(Convert.FromBase64String(encrypted), pass_kdf, pass_IV);
				users_infos = JsonConvert.DeserializeObject<Dictionary<string, UserInfo>>(json);
				MainClass mc = GameObject.Find("Canvas").GetComponent<MainClass>();
				foreach (var ui in users_infos.Values) {
					mc.AddContactToList(ui.nickname, ui.id);
				}
			} catch (Exception ex) {
				users_infos ??= new Dictionary<string, UserInfo>();
				Debug.Log("cannot load users_infos : " + ex.Message);
			}
		}

		string roots_path = Application.streamingAssetsPath + "/" + nickname + "_messages_roots.txt";
		if (File.Exists(roots_path)) {
			try {

				string encrypted = File.ReadAllText(roots_path);
				string json = Crypto.DecryptAES(Convert.FromBase64String(encrypted), pass_kdf, pass_IV);
				root_memory = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
			} catch (Exception ex) {
				root_memory ??= new Dictionary<string, string>();
				Debug.Log("cannot load root memory : " + ex.Message);
			}
		}
	}
}

/// <summary>
/// Contain info about other users
/// sending and receiving ratchets, nick and ID
/// </summary>
public class UserInfo {
	public string nickname;
	public string id;

	public Ratchet sending_ratchet;
	public Ratchet receiving_ratchet;
}

/// <summary>
/// Used to encrypted plain text, from a root
/// each increment deriv the root with a KDF function
/// </summary>
public class Ratchet {
	public int index = 0;
	public string rsa_public = "";
	public string WARNING__rsa_private = "";
	public string root = "";
	public string root_id = "";
	DateTime lastUpdate = DateTime.MinValue;

	public Ratchet Init(string rsa_pu, string rsa_pr) {
		var hash = Crypto.GenerateRandomHash();

		index = 1;
		root = Convert.ToBase64String(hash);
		root_id = Guid.NewGuid().ToString();
		WARNING__rsa_private = rsa_pr;
		rsa_public = rsa_pu;
		return this;
	}

	//renew the issue ratchet with random root
	//it will be communicated to the next message we send, with the public RSA provided by the other user
	public void Renew(string rsa_pu, DateTime date) {
		if (date < lastUpdate) {
			Debug.Log("don't init a ratchet with an old message !");
			return;
		}
		lastUpdate = date;
		rsa_public = rsa_pu;
		WARNING__rsa_private = "ukn";
		index = 1;
		var hash = Crypto.GenerateRandomHash();
		root = Convert.ToBase64String(hash);
		root_id = Guid.NewGuid().ToString();
		User.root_memory.Add(root_id, root);
	}

	//get our receiving root thanks to the RSA key we sent before
	//prepare a new RSA key to receive a new root when the user answers us
	public bool Set(string new_root, string new_root_id, DateTime date) {
		if (date < lastUpdate) {
			Debug.Log("don't init a ratchet with an old message !");
			return false;
		}
		lastUpdate = date;
		bool success = true;
		if (WARNING__rsa_private == "") throw new Exception("Error : RSA private key not loaded");
		try {
			root = Crypto.DecryptionRSA(new_root, WARNING__rsa_private);
			User.root_memory.Add(new_root_id, root);
		} catch (Exception ex) {
			Debug.Log("Erreur RSA, cannot decrypt " + new_root + " with " + WARNING__rsa_private + "'" + ex.Message);
			success = false;
		}
		//then we generate another RSA for the next sending
		PrepareNextRSA(); 
		return success;
	}

	/// <summary>
	/// generate a new RSA keypair to tell foreign 
	/// user on wich key they can encrypt ratchet info
	/// </summary>
	public void PrepareNextRSA() {
		RSACryptoServiceProvider dedicaced_rsa = new RSACryptoServiceProvider();
		rsa_public = Crypto.Simplfy_XML_RSA(dedicaced_rsa.ToXmlString(false));
		WARNING__rsa_private = dedicaced_rsa.ToXmlString(true);
	}
}

/// <summary>
/// This contains all info of message, and methods to perform decryption
/// </summary>
public class Message {
	public string id;
	public string from;
	public string to;
	public string cipher;
	public string ratchet_infos;
	public int ratchet_index;
	public string root_id;
	public string sender_rsa_info;
	public DateTime send_at;
	public bool distributed;
	public string plain = "";
	public bool decrypted = false;

	/// <summary>
	/// Convert date from js format to a c# object (with miliseconds)
	/// </summary>
	public void SetDate(string js_date) {
		//2023-01-31T22:34:54.000Z
		try {
			string format = "yyyy-MM-ddTHH:mm:ss.fffZ";
			send_at = DateTime.ParseExact(js_date, format, CultureInfo.InvariantCulture);
		} catch (Exception e) {
			Debug.Log("Error : " + e.Message + " (" + js_date + ")");
		}
	}

	/// <summary>
	/// decrypt a message with his given root and kdf index
	/// </summary>
	public void Decrypt() {
		plain = "<#FF0000>Invalid key for this message";
		if (decrypted) {
			return;
		}

		if (!User.root_memory.ContainsKey(root_id)) {
			return;
		}
		try {
			var root = User.root_memory[root_id];
			var root_bytes = Convert.FromBase64String(root);
			//salt derive from ID
			var salt = Crypto.Hash(Encoding.UTF8.GetBytes(id));
			//initialization vector derive from root_id
			var IV = Crypto.Hash(Encoding.UTF8.GetBytes(root_id));
			var kdf_result = Crypto.KDF(root_bytes, salt, ratchet_index);
			plain = Crypto.DecryptAES(Crypto.ToObfuscatedBytes(cipher), kdf_result, IV);
			decrypted = true;
			cipher = "[DECRYPTED]";
		} catch (Exception ex) {
			decrypted = false;
			plain = ex.Message;
		}
	}

	/// <summary>
	/// encrypt a message with associated root and kdf index
	/// </summary>
	public void Encrypt(string root, int ticks) {
		var root_bytes = Convert.FromBase64String(root);
		var salt = Crypto.Hash(Encoding.UTF8.GetBytes(id));
		var IV = Crypto.Hash(Encoding.UTF8.GetBytes(root_id));
		var kdf_result = Crypto.KDF(root_bytes, salt, ticks);
		cipher = Crypto.FromObfuscatedByte(Crypto.EncryptAES(plain, kdf_result, IV));
		plain = "encrypted";
	}
}


