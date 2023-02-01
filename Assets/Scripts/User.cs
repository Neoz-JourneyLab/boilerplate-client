using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using UnityEngine;

public static class User {
	public static string nickname;
	public static string id;
	public static Dictionary<string, UserInfo> users_infos = new Dictionary<string, UserInfo>();
	public static Dictionary<string, List<Message>> conversations = new Dictionary<string, List<Message>>();
	public static Dictionary<string, string> messageId_root = new Dictionary<string, string>();
	static string default_private_key;

	public static void InitPrivateKey() {
		if (File.Exists(Application.streamingAssetsPath + "/" + nickname + "_default_private_key.txt")) {
			default_private_key = File.ReadAllText(Application.streamingAssetsPath + "/" + nickname + "_default_private_key.txt");
		} else {
			default_private_key = new RSACryptoServiceProvider().ToXmlString(true);
			File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_default_private_key.txt", default_private_key);
		}
	}

	public static string GetDefaultPublicKey() {
		var tmp = new RSACryptoServiceProvider();
		tmp.FromXmlString(default_private_key);
		return tmp.ToXmlString(false);
	}

	public static string WARNING___GetDefaultPrivateKey() {
		var tmp = new RSACryptoServiceProvider();
		tmp.FromXmlString(default_private_key);
		return Crypto.Simplfy_XML_RSA(tmp.ToXmlString(true));
	}

	public static void SaveData(string data) {
		if (data.Contains("messages")) {
			string c = JsonConvert.SerializeObject(conversations, Formatting.Indented);
			//File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_conversations.txt", c);
		}
		if (data.Contains("infos")) {
			string ui = JsonConvert.SerializeObject(users_infos, Formatting.Indented);
			File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_user_infos.txt", ui);
		}
		if (data.Contains("roots")) {
			string rt = JsonConvert.SerializeObject(messageId_root, Formatting.Indented);
			File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_messages_roots.txt", rt);
		}
	}

	public static void LoadData() {
		if (File.Exists(Application.streamingAssetsPath + "/" + nickname + "_user_infos.txt")) {
			users_infos = JsonConvert.DeserializeObject<Dictionary<string, UserInfo>>(File.ReadAllText(Application.streamingAssetsPath + "/" + nickname + "_user_infos.txt"));
			if (users_infos == null) users_infos = new Dictionary<string, UserInfo>();
			MainClass mc = GameObject.Find("Canvas").GetComponent<MainClass>();
			foreach (var ui in users_infos.Values) {
				mc.AddContactToList(ui.nickname, ui.id);
			}
		}
		if (File.Exists(Application.streamingAssetsPath + "/" + nickname + "_conversations.txt")) {
			conversations = JsonConvert.DeserializeObject<Dictionary<string, List<Message>>>(File.ReadAllText(Application.streamingAssetsPath + "/" + nickname + "_conversations.txt"));
			if (conversations == null) conversations = new Dictionary<string, List<Message>>();
		}
		if (File.Exists(Application.streamingAssetsPath + "/" + nickname + "_messages_roots.txt")) {
			messageId_root = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Application.streamingAssetsPath + "/" + nickname + "_messages_roots.txt"));
			if (messageId_root == null) messageId_root = new Dictionary<string, string>();
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

	public void Renew(DateTime val, string rsa_pu, string rsa_pr) {
		rsa_public = rsa_pu;
		WARNING__rsa_private = "ukn";
		valitidy = val;
		index = 1;
		var hash = Crypto.GenerateRandomHash();
		root = Convert.ToBase64String(hash);
	}

	public void PrepareNextRSA() {
		RSACryptoServiceProvider dedicaced_rsa = new RSACryptoServiceProvider();
		rsa_public = dedicaced_rsa.ToXmlString(false);
		WARNING__rsa_private = dedicaced_rsa.ToXmlString(true);
	}

	public void Set(string new_root, DateTime val) {
		root = new_root;
		//(root a decoder avec private RSA)
		valitidy = val;
		byte[] hash = Convert.FromBase64String(root);
		PrepareNextRSA();
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
		if (plain != "") return;
		int ticks = int.Parse(ratchet_infos.Split(' ')[1]);

		if(!User.messageId_root.ContainsKey(id)) {
			plain = "<#FF0000>missing ratchet root for this message";
			return;
		}

		var root = User.messageId_root[id];
		var salt = Crypto.Hash(Encoding.UTF8.GetBytes(id));
		var IV = Crypto.KDF(salt, Convert.FromBase64String(root), salt[6]);
		var kdf = Crypto.KDF(Convert.FromBase64String(root), salt, ticks);
		plain = Crypto.DecryptAES(Convert.FromBase64String(cipher), kdf, IV);
		
		/*
		cipher += "\n<#FF00C0>root avec " + User.messageId_root[id].Substring(0, 5);

		// si le ratchet ne tiens pas d'infos, on ne met rien à jour
		if (!ratchet_infos.StartsWith("_")) {
			//si il possède des infos, on met à jour
			if (from_us) {
				//si on envoie le premier message, il faut initialiser notre envoi avec la clé RSA par défaut de l'utilisateur
				string sending_rsa = first_message ? User.users_infos[GetFgnId()].default_public_rsa : User.users_infos[GetFgnId()].sending_ratchet.rsa_public;
				User.users_infos[GetFgnId()].receiving_ratchet.rsa_public = sender_rsa_info; //il faut aussi récupéré la clé privée
				cipher += "<#00FF00>\non recevra futur root avec la clé [RSA " + sender_rsa_info.Substring(0, 5) + "]";

				User.users_infos[GetFgnId()].sending_ratchet.root = ratchet_infos.Split(' ')[0]; //on a envoyé notre nouveau root d'émission chiffré avec la RSA publique, il faut le retrouver déchiffré
				cipher += "\non envoie partir de maintenant avec le root " + ratchet_infos.Split(' ')[0].Substring(0, 5) + " encrypté avec le RSA " + sending_rsa.Substring(0, 5);

			} else {
				cipher += "<#00FFFF>\non enverra notre root d'émission avec la clé RSA de l'autre: " + sender_rsa_info.Substring(0, 5);
				User.users_infos[GetFgnId()].sending_ratchet.rsa_public = sender_rsa_info;

				cipher += "\non recoit avec ses infos décodée par notre précédent RSA " + User.users_infos[GetFgnId()].receiving_ratchet.rsa_public.Substring(0, 5);
				cipher += "\nle root sera " + ratchet_infos.Split(' ')[0].Substring(0, 5);
				User.users_infos[GetFgnId()].receiving_ratchet.root = ratchet_infos.Split(' ')[0]; //décodé par rsa
			}
		}

		if (from_us) {
			cipher += "<#FFFF00>\ndecodé avec " + User.users_infos[GetFgnId()].sending_ratchet.root.Substring(0, 5) + " > " + ticks;
		} else {
			cipher += "<#FFFF00>\ndecodé avec " + User.users_infos[GetFgnId()].receiving_ratchet.root.Substring(0, 5) + " > " + ticks;
		}
		*/
	}

	public void Encrypt(string root, int ticks) {
		var salt = Crypto.Hash(Encoding.UTF8.GetBytes(id));
		var IV = Crypto.KDF(salt, Convert.FromBase64String(root), salt[6]);
		var kdf = Crypto.KDF(Convert.FromBase64String(root), salt, ticks);
		cipher = Convert.ToBase64String(Crypto.EncryptAES(plain, kdf, IV));
		plain = "encrypted";
	}

	string GetFgnId() {
		return from == User.id ? to : from;
	}
}


