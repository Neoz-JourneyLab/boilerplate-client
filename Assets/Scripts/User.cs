using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class User {
	public static string nickname;
	public static string id;
	public static byte[] pass_kdf;
	public static byte[] pass_IV;
	public static Dictionary<string, UserInfo> users_infos = new Dictionary<string, UserInfo>();
	public static Dictionary<string, List<Message>> conversations = new Dictionary<string, List<Message>>();
	public static Dictionary<string, string> messageId_root = new Dictionary<string, string>();
	static string default_private_key;
	public static Dictionary<string, DateTime> lastSaves = new Dictionary<string, DateTime>();

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

	public static void SaveData(string data) {
		if (data.Contains("messages")) {
			if (lastSaves.ContainsKey("messages") && (DateTime.UtcNow - lastSaves["messages"]).TotalSeconds < 30) {
				//Debug.Log("pas de sauvegarde : on a save messages y'a " + (DateTime.UtcNow - lastSaves["messages"]).TotalSeconds + " s");
			} else {
				lastSaves["messages"] = DateTime.UtcNow;
				string c = JsonConvert.SerializeObject(conversations, Formatting.Indented);
				//File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_conversations.txt", c);
			}
		}
		if (data.Contains("infos")) {
			if (lastSaves.ContainsKey("infos") && (DateTime.UtcNow - lastSaves["infos"]).TotalSeconds < 30) {
				//Debug.Log("pas de sauvegarde : on a save infos y'a " + (DateTime.UtcNow - lastSaves["infos"]).TotalSeconds + " s");
			} else {
				lastSaves["infos"] = DateTime.UtcNow;
				//on ne save pas les infos si il n'y a aucun messages : il y a eu une erreur de com
				var infos = users_infos.Where(u => conversations.ContainsKey(u.Key)).ToDictionary(i => i.Key, i => i.Value);
				string ui = JsonConvert.SerializeObject(infos, Formatting.Indented);
				string encrypted = Convert.ToBase64String(Crypto.EncryptAES(ui, pass_kdf, pass_IV));
				File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_user_infos.txt", encrypted);
				File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_user_infos_CLEAR.txt", ui);
			}
		}
		if (data.Contains("roots")) {
			if (lastSaves.ContainsKey("roots") && (DateTime.UtcNow - lastSaves["roots"]).TotalSeconds < 30) {
				//Debug.Log("pas de sauvegarde : on a save roots y'a " + (DateTime.UtcNow - lastSaves["roots"]).TotalSeconds + " s");
			} else {
				lastSaves["roots"] = DateTime.UtcNow;
				string rt = JsonConvert.SerializeObject(messageId_root, Formatting.Indented);
				string encrypted = Convert.ToBase64String(Crypto.EncryptAES(rt, pass_kdf, pass_IV));
				File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_messages_roots.txt", encrypted);
				File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_messages_roots_CLEAR.txt", rt);
			}
		}
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
			}
		}

		if (File.Exists(Application.streamingAssetsPath + "/" + nickname + "_conversations.txt")) {
			try {
				string encrypted = File.ReadAllText(Application.streamingAssetsPath + "/" + nickname + "_conversations.txt");
				conversations = JsonConvert.DeserializeObject<Dictionary<string, List<Message>>>(Crypto.DecryptAES(Convert.FromBase64String(encrypted), pass_kdf, pass_IV));
			} catch (Exception ex) {
				conversations ??= new Dictionary<string, List<Message>>();
			}
		}
		if (File.Exists(Application.streamingAssetsPath + "/" + nickname + "_messages_roots.txt")) {
			try {

				string encrypted = File.ReadAllText(Application.streamingAssetsPath + "/" + nickname + "_messages_roots.txt");
				messageId_root = JsonConvert.DeserializeObject<Dictionary<string, string>>(Crypto.DecryptAES(Convert.FromBase64String(encrypted), pass_kdf, pass_IV));
			} catch (Exception ex) {
				messageId_root ??= new Dictionary<string, string>();

			}
		}
	}
}

public class UserInfo {
	public string nickname;
	public string id;
	public string default_public_rsa;
	public bool isInit = false;

	public Ratchet sending_ratchet;
	public Ratchet receiving_ratchet;
}

public class Ratchet {
	public DateTime valitidy;
	public int index;
	public string rsa_public;
	public string WARNING__rsa_private;
	public string root;

	public Ratchet Init(string r, string rsa_pu, string rsa_pr, DateTime val) {
		valitidy = val;
		index = 1;
		root = r;
		byte[] hash = Convert.FromBase64String(r);
		WARNING__rsa_private = rsa_pr;
		rsa_public = rsa_pu;
		return this;
	}

	//renouvelle le ratchet d'émission avec un root aléatoire
	//il sera communiqué au prochain message qu'on enverra, avec la RSA publique fournie par l'autre user
	public void Renew(DateTime val, string rsa_pu) {
		rsa_public = rsa_pu;
		WARNING__rsa_private = "ukn";
		valitidy = val;
		index = 1;
		var hash = Crypto.GenerateRandomHash();
		root = Convert.ToBase64String(hash);
	}

	//récupère notre root de réception grace à la clé RSA qu'on avait envoyé auparavant
	//prépare une nouvelle clé RSA pour recevoir un nouveau root quand l'user nous répondra
	public void Set(string new_root, DateTime val) {
		//(root a decoder avec private RSA précédent)
		try {
			root = Crypto.DecryptionRSA(new_root, WARNING__rsa_private);
			valitidy = val;
			PrepareNextRSA(); // puis on génère un autre RSA pour le prochain envoi
		} catch(Exception ex) {
			//si on pense recevoir avec notre RSA par défaut par exemple
			Debug.Log("Mauvais RSA !");
		}
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
	public string sender_rsa_info;
	public DateTime send_at;
	public bool distributed;
	public string plain = "";
	public bool decrypted = false;

	public void SetDate(string js_date) {
		//30/01/2023 14:54:29 || 2023-01-31T22:34:54.000Z
		try {
			string format = js_date.EndsWith("000Z") ? "yyyy-MM-ddTHH:mm:ss.fffZ" : "dd/MM/yyyy HH:mm:ss";
			send_at = DateTime.ParseExact(js_date, format, CultureInfo.InvariantCulture);
		} catch (Exception e) {
			Debug.Log("Error : " + e.Message + " (" + js_date + ")");
		}
	}

	public void Decrypt() {
		if (decrypted) {
			return;
		}
		int ticks = int.Parse(ratchet_infos.Split(' ')[1]);

		//si on recoit un message sans infos de root, c'est qu'elle se situe sur un message en amont.
		if (!User.messageId_root.ContainsKey(id)) {
			plain = "<#FF0000>missing ratchet root for this message";
			return;
		}
		try {
			var root = User.messageId_root[id];
			if (root.Split('-').Length == 5) {
				plain = "<#FF0000>root is user ID";
				Debug.Log("Le root est un UUID d'user : " + root);
				return;
			}

			var root_bytes = Convert.FromBase64String(root);
			var salt = Crypto.Hash(Encoding.UTF8.GetBytes(id));
			var IV = Crypto.Hash(salt);
			var kdf = Crypto.KDF(root_bytes, salt, ticks);
			plain = Crypto.DecryptAES(Convert.FromBase64String(cipher), kdf, IV);
			decrypted = true;
		} catch (Exception ex) {
			plain = "<#FFF000>erreur : " + ex.Message;
			decrypted = false;
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


