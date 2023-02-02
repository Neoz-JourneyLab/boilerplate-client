using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using WebSocketSharp;

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

	public static void InitPrivateKey() {
		if (File.Exists(Application.streamingAssetsPath + "/" + nickname + "_default_private_key.txt")) {
			string pk = File.ReadAllText(Application.streamingAssetsPath + "/" + nickname + "_default_private_key.txt");
			default_private_key = Crypto.DecryptAES(Convert.FromBase64String(pk), pass_kdf, pass_IV);
		} else {
			default_private_key = new RSACryptoServiceProvider().ToXmlString(true);
			string pk = Convert.ToBase64String(Crypto.EncryptAES(default_private_key, pass_kdf, pass_IV));
			File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_default_private_key.txt", pk);
		}
	}

	public static string GetDefaultPublicKey() {
		var tmp = new RSACryptoServiceProvider();
		tmp.FromXmlString(default_private_key);
		return Crypto.Simplfy_XML_RSA(tmp.ToXmlString(false));
	}

	public static string WARNING___GetDefaultPrivateKey() {
		var tmp = new RSACryptoServiceProvider();
		tmp.FromXmlString(default_private_key);
		return tmp.ToXmlString(true);
	}

	public static void SaveData(bool force = false) {
		if(force) {
			lastSaves.Clear();
		}
		Save("user_infos.txt", users_infos);
		Save("messages_roots.txt", root_memory);
	}

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

	public static void LoadData() {
		if (File.Exists(Application.streamingAssetsPath + "/" + nickname + "_user_infos.txt")) {
			try {
				string encrypted = File.ReadAllText(Application.streamingAssetsPath + "/" + nickname + "_user_infos.txt");
				users_infos = JsonConvert.DeserializeObject<Dictionary<string, UserInfo>>(Crypto.DecryptAES(Convert.FromBase64String(encrypted), pass_kdf, pass_IV));
				MainClass mc = GameObject.Find("Canvas").GetComponent<MainClass>();
				foreach (var ui in users_infos.Values) {
					mc.AddContactToList(ui.nickname, ui.id);
				}
			} catch (Exception ex) {
				users_infos ??= new Dictionary<string, UserInfo>();
				Debug.Log("cannot load users_infos : " + ex.Message);
			}
		}

		if (File.Exists(Application.streamingAssetsPath + "/" + nickname + "_messages_roots.txt")) {
			try {

				string encrypted = File.ReadAllText(Application.streamingAssetsPath + "/" + nickname + "_messages_roots.txt");
				root_memory = JsonConvert.DeserializeObject<Dictionary<string, string>>(Crypto.DecryptAES(Convert.FromBase64String(encrypted), pass_kdf, pass_IV));
			} catch (Exception ex) {
				root_memory ??= new Dictionary<string, string>();
				Debug.Log("cannot load root memory : " + ex.Message);
			}
		}
	}
}

public class UserInfo {
	public string nickname;
	public string id;
	public string default_public_rsa;

	public Ratchet sending_ratchet;
	public Ratchet receiving_ratchet;
}

public class Ratchet {
	public int index = 0;
	public string rsa_public = "";
	public string WARNING__rsa_private = "";
	public string root = "";
	public string root_id = "";
	DateTime lastUpdate = DateTime.MinValue;

	public Ratchet Init(string r, string rid, string rsa_pu, string rsa_pr) {
		index = 1;
		root = r;
		root_id = rid;
		WARNING__rsa_private = rsa_pr;
		rsa_public = rsa_pu;
		return this;
	}

	//renouvelle le ratchet d'émission avec un root aléatoire
	//il sera communiqué au prochain message qu'on enverra, avec la RSA publique fournie par l'autre user
	public void Renew(string rsa_pu, DateTime date) {
		if(date < lastUpdate) {
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
		//Debug.Log("Ajout d'un root ID pour notre nouveau root d'émission : " + root);
	}

	//récupère notre root de réception grace à la clé RSA qu'on avait envoyé auparavant
	//prépare une nouvelle clé RSA pour recevoir un nouveau root quand l'user nous répondra
	public bool Set(string new_root, string new_root_id, DateTime date) {
		if (date < lastUpdate) {
			Debug.Log("don't init a ratchet with an old message !");
			return false;
		}
		lastUpdate = date;
		bool success = true;
		if (WARNING__rsa_private == "") throw new Exception("Error : RSA private key not loaded");
		//(root a decoder avec private RSA précédent)
		try {
			root = Crypto.DecryptionRSA(new_root, WARNING__rsa_private);
			User.root_memory.Add(new_root_id, root);
			//Debug.Log("nous savons désormais écouter les root " + new_root_id + " > " + root);
		} catch (Exception ex) {
			//si on pense recevoir avec notre RSA par défaut par exemple
			Debug.Log("Erreur RSA, cannot decrypt " + new_root + " with " + WARNING__rsa_private + "'" + ex.Message);
			success = false;
		}
		PrepareNextRSA(); // puis on génère un autre RSA pour le prochain envoi
		return success;
	}

	public void PrepareNextRSA() {
		RSACryptoServiceProvider dedicaced_rsa = new RSACryptoServiceProvider();
		rsa_public = Crypto.Simplfy_XML_RSA(dedicaced_rsa.ToXmlString(false));
		WARNING__rsa_private = dedicaced_rsa.ToXmlString(true);
	}
}

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

	public void SetDate(string js_date) {
		//2023-01-31T22:34:54.000Z
		try {
			string format = "yyyy-MM-ddTHH:mm:ss.fffZ";
			send_at = DateTime.ParseExact(js_date, format, CultureInfo.InvariantCulture);
		} catch (Exception e) {
			Debug.Log("Error : " + e.Message + " (" + js_date + ")");
		}
	}

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
			var salt = Crypto.Hash(Encoding.UTF8.GetBytes(id));
			var IV = Crypto.Hash(salt);
			var kdf = Crypto.KDF(root_bytes, salt, ratchet_index);
			plain = Crypto.DecryptAES(Convert.FromBase64String(cipher), kdf, IV);
			decrypted = true;
		} catch (Exception ex) {
			decrypted = false;
			plain = ex.Message;
		}
	}

	public void Encrypt(string root, int ticks) {
		var root_bytes = Convert.FromBase64String(root);
		var salt = Crypto.Hash(Encoding.UTF8.GetBytes(id));
		var IV = Crypto.Hash(salt);
		var kdf = Crypto.KDF(root_bytes, salt, ticks);
		cipher = Convert.ToBase64String(Crypto.EncryptAES(plain, kdf, IV));
		plain = "encrypted";
	}

	string GetFgnId() {
		return from == User.id ? to : from;
	}
}


