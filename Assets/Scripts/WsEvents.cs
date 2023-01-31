using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Tools;

/// <summary>
/// Contient tous les évenements Uws
/// </summary>
public class WsEvents : MonoBehaviour {

	#region Vars

	static int latency;
	public static readonly Dictionary<string, DateTime> pings = new Dictionary<string, DateTime>();
	static TMP_Text serverStatus;

	#endregion

	#region listeners
	public static void Pong(string json) {
		string pong = JObject.Parse(json)["ping_id"].ToString();
		string serverTime = JObject.Parse(json)["server_time"].ToString();
		if (!pings.ContainsKey(pong)) throw new Exception("ping ID not found: " + pong);
		latency = (int)(DateTime.UtcNow - pings[pong]).TotalMilliseconds / 2;
		pings.Remove(pong);
		if (serverStatus == null) {
			serverStatus = GameObject.Find("server infos").GetComponent<TMP_Text>();
		}
		serverStatus.color = ColorPalette.Get(Palette.paleBlue);
		serverStatus.text = $"server : {GetDateFromStr(serverTime).ToShortDateString() + " " + GetDateFromStr(serverTime).ToLongTimeString()}, {latency}ms";
	}
	public static void UserInfos(string json) {
		if (json.StartsWith("{\"err\"")) {
			string err = JObject.Parse(json)["err"].ToString();
			GameObject.Find("Canvas").GetComponent<MainClass>().Prompt(err);
			return;
		}
		string id = JObject.Parse(json)["id"].ToString();
		string nick = JObject.Parse(json)["nickname"].ToString();
		string rsa = JObject.Parse(json)["default_public_rsa"].ToString();
		if (User.users_infos.ContainsKey(id)) return;

		//réception des informations de l'utilisateur, on lui créer un ratchet d'émission
		//on assigne un ratchet de réception (peu importe la seed qui sera initialisée à la reception du premier message)
		//on lui assigne un RSA dédié pour qu'il nous envoie ses informations d'émission
		var r = Crypto.GenerateRandomHash();
		var s = Crypto.GenerateRandomHash();
		RSACryptoServiceProvider dedicaced_rsa = new();
		User.users_infos[id] = new UserInfo() {
			id = id,
			nickname = nick,
			sending_ratchet = new Ratchet(Convert.ToBase64String(s), Convert.ToBase64String(r), rsa, "ukn"),
			receiving_ratchet = new Ratchet(Convert.ToBase64String(s), Convert.ToBase64String(r), dedicaced_rsa.ToXmlString(false), dedicaced_rsa.ToXmlString(true)),
		};

		GameObject.Find("Canvas").GetComponent<MainClass>().AddContactToList(nick, id);
		User.SaveData("infos");
	}

	public static void NewMessage(string json) {
		string send_at = JObject.Parse(json)["send_at"].ToString();

		Message m = JsonConvert.DeserializeObject<Message>(json);
		m.SetDate(send_at);

		string fgn = m.to == User.id ? m.from : m.to;
		if (!User.conversations.ContainsKey(fgn)) {
			User.conversations.Add(fgn, new List<Message>() { m });
		} else {
			User.conversations[fgn].Add(m);
		}

		//si on a pas les infos de l'utilisateur, on les ajoute
		if (!User.users_infos.ContainsKey(fgn)) {
			Debug.Log("Création du ratchet d'émission");
			var r = Crypto.GenerateRandomHash();
			var s = Crypto.GenerateRandomHash();
			RSACryptoServiceProvider my_rsa = new RSACryptoServiceProvider();
			User.users_infos[fgn] = new UserInfo(Convert.ToBase64String(r), Convert.ToBase64String(s)) {
				id = fgn,
				nickname = m.fgn_nick,
				current_public_rsa = m.new_public_rsa,
				current_public_rsa_date = m.send_at,
				my_private_rsa = "default", //on utilise notre clé RSA par défaut pour la réception des premiers messages
				my_public_rsa = User.GetDefaultPublicKey(),
			};
		} else if(m.send_at > User.users_infos[fgn].current_public_rsa_date) {
			Debug.Log("MAJ du ratchet d'émission");
			//si on a déjà les infos, on update son RSA d'émission (si c'est le plus récent)
			User.users_infos[fgn].current_public_rsa = User.default_public_key;
			User.users_infos[fgn].current_public_rsa_date = User.default_private_key;
			var r = Crypto.GenerateRandomHash();
			var s = Crypto.GenerateRandomHash();
			//et on réinitialise le ratchet (qui sera renvoyé au prochain message)
			User.users_infos[fgn].sending_ratchet = new Ratchet(Convert.ToBase64String(r), Convert.ToBase64String(s));
		} else {
			Debug.Log("Ratchet d'émission à jour.");
		}

		User.SaveData("messages infos");
		GameObject.Find("Canvas").GetComponent<MessageManager>().LoadConv();
	}

	public static void AuthOK(string json) {
		string user_id = JObject.Parse(json)["user_id"].ToString();
		User.id = user_id;
		User.LoadData();
		GameObject.Find("auth error").GetComponent<TMP_Text>().text = "";
		GameObject.Find("AuthGroup").SetActive(false);
		uWebSocketManager.EmitEv("request:conversations");
	}

	public static void AuthError(string json) {
		Debug.Log(json);
		GameObject.Find("auth error").GetComponent<TMP_Text>().text = "AUTH ERROR : " + json;
	}

	public static void Err(string json) {
		string code = JObject.Parse(json)["code"].ToString();
		string message = JObject.Parse(json)["message"].ToString();
		Debug.Log("Error : " + code + " > " + message);
	}
	#endregion
}

