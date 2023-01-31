using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

public static class User {
	public static string nickname;
	public static string id;
	public static Dictionary<string, UserInfo> users_infos = new Dictionary<string, UserInfo>();
	public static Dictionary<string, List<Message>> conversations = new Dictionary<string, List<Message>>();
	public static string default_private_key;

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
		return tmp.ToXmlString(true);
	}

	public static void SaveData(string data) {
		if (data.Contains("messages")) {
			string c = JsonConvert.SerializeObject(conversations, Formatting.Indented);
			File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_conversations.txt", c);
		} else if (data.Contains("infos")) {
			string ui = JsonConvert.SerializeObject(users_infos, Formatting.Indented);
			File.WriteAllText(Application.streamingAssetsPath + "/" + nickname + "_user_infos.txt", ui);
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
	}
}

public class UserInfo {
	public string nickname;
	public string id;

	public Ratchet sending_ratchet;
	public Ratchet receiving_ratchet;
	public bool update_our_at_next_message = false;
}

public class Ratchet {
	public DateTime valitidy;
	public int index;
	public string rsa_public;
	public string WARNING__rsa_private;
	public string root;
	public string salt;

	public Ratchet(string r, string s, string rsa_pu, string rsa_pr) {
		index = 1;
		root = r;
		salt = s;
		WARNING__rsa_private = rsa_pr;
		rsa_public = rsa_pu;
	}
}

public class Message {
	public string from;
	public string fgn_nick;
	public string to;
	public string cipher;
	public string ratchet_infos;
	public string new_public_rsa;
	public DateTime send_at;
	public bool distributed;

	public void SetDate(string js_date) {
		try { //30/01/2023 14:54:29
			send_at = DateTime.ParseExact(js_date, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
		} catch (Exception e) {
			Debug.Log("Error : " + e.Message + " (" + js_date + ")");
		}
	}
}
