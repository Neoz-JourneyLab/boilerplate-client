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
	static readonly MessageManager MM = GameObject.Find("Canvas").GetComponent<MessageManager>();
	public static List<string> noPreviousMessageFromThosesUsers = new List<string>();
	#endregion

	#region listeners

	public static TMP_Text GetServerStatusTxt() {
		if (serverStatus == null) {
			serverStatus = GameObject.Find("server infos").GetComponent<TMP_Text>();
		}
		return serverStatus;
	}

	public static void Pong(string json) {
		string pong = JObject.Parse(json)["ping_id"].ToString();
		string serverTime = JObject.Parse(json)["server_time"].ToString();
		if (!pings.ContainsKey(pong)) throw new Exception("ping ID not found: " + pong);
		latency = (int)(DateTime.UtcNow - pings[pong]).TotalMilliseconds / 2;
		pings.Remove(pong);
		if (serverStatus == null) {
			serverStatus = GameObject.Find("server infos").GetComponent<TMP_Text>();
		}
		float a = serverStatus.color.a;
		serverStatus.color = ColorPalette.Get(Palette.paleBlue, a);
		serverStatus.text = Languages.Get("server") + $" : {GetDateFromStr(serverTime).ToShortDateString() + " " + GetDateFromStr(serverTime).ToLongTimeString()} (ping {latency}ms)";
	}
	public static void UserInfos(string json) {
		if (json.StartsWith("{\"err\"")) {
			string err = JObject.Parse(json)["err"].ToString();
			GameObject.Find("Canvas").GetComponent<MainClass>().Prompt(err);
			return;
		}
		string id = JObject.Parse(json)["id"].ToString();
		string nick = JObject.Parse(json)["nickname"].ToString();
		string foreign_default_rsa = JObject.Parse(json)["default_public_rsa"].ToString();

		//requete forcée par reception d'un premier message
		if (User.users_infos.ContainsKey(id) && User.users_infos[id].nickname == "") {
			User.users_infos[id].default_public_rsa = Crypto.Simplfy_XML_RSA(foreign_default_rsa);
			User.users_infos[id].nickname = nick;
			User.users_infos[id].receiving_ratchet.WARNING__rsa_private = User.WARNING___GetDefaultPrivateKey();

			GameObject.Find("Canvas").GetComponent<MainClass>().AddContactToList(nick, id);
			TMP_Text txt = GameObject.Find("contact_" + id).transform.Find("nick").GetComponent<TMP_Text>();
			txt.color = new Color(1f, .63f, 0);
			User.SaveData();
			return;
		}

		//réception volontaire des informations de l'utilisateur, on lui créer un ratchet d'émission
		//on assigne un ratchet de réception (peu importe la seed qui sera initialisée à la reception du premier message)
		//on lui assigne un RSA dédié pour qu'il nous envoie ses informations d'émission
		var hash = Crypto.GenerateRandomHash();
		RSACryptoServiceProvider dedicaced_rsa = new RSACryptoServiceProvider();

		User.users_infos[id] = new UserInfo() {
			id = id,
			nickname = nick,
			sending_ratchet = new Ratchet().Init(Convert.ToBase64String(hash), Guid.NewGuid().ToString(), foreign_default_rsa, "ukn"),
			receiving_ratchet = new Ratchet().Init(Convert.ToBase64String(Crypto.GenerateRandomHash()), Guid.NewGuid().ToString(), Crypto.Simplfy_XML_RSA(dedicaced_rsa.ToXmlString(false)), dedicaced_rsa.ToXmlString(true)),
		};

		User.root_memory.Add(User.users_infos[id].sending_ratchet.root_id, User.users_infos[id].sending_ratchet.root);

		User.users_infos[id].default_public_rsa = Crypto.Simplfy_XML_RSA(foreign_default_rsa);
		MM.Focus(id);

		//il faut à présent notifier l'utilisateur distant du ROOT qu'on lui a attribué.
		//sinon, si il nous ajoute à son tour sans qu'on lui envoie un message, il nous parlera sur notre RSA par défaut
		//c'est lors de la réception de ce message qu'on pourra être sur que la com est établie
		MM.SendFirstMessage();

		GameObject.Find("Canvas").GetComponent<MainClass>().AddContactToList(nick, id);
	}

	static bool logs = false;
	public static void NewMessage(string json) {
		var settings = new JsonSerializerSettings {
			DateParseHandling = DateParseHandling.None
		};
		JObject jObj = JsonConvert.DeserializeObject<JObject>(json, settings);
		string send_at = jObj.SelectToken("send_at").ToString();
		string pending_batch = JObject.Parse(json)["pending_batch"].ToString();

		Message m = JsonConvert.DeserializeObject<Message>(json);
		m.SetDate(send_at);

		if (MM.wait_for_send_next == m.id) {
			MM.CanSend();
		}

		string fgn = m.to == User.id ? m.from : m.to;

		bool isLastMessage = false;

		if (logs) print("réception du message " + m.id + ", " + pending_batch + ", root ID " + m.root_id);

		//ajout du message dans la liste
		if (!User.conversations.ContainsKey(fgn)) {
			//ici, il s'agit du premier message reçu (donc du dernier envoyé)
			User.conversations.Add(fgn, new List<Message>() { m });
		} else if (User.conversations[fgn].Find(mes => mes.id == m.id) == null) {
			DateTime lastMessage = User.conversations[fgn].Last().send_at;
			DateTime firstMessage = User.conversations[fgn].First().send_at;

			//si il s'agit d'un message plus récent que tous, on l'ajoute simplement au début
			if (firstMessage > m.send_at) {
				User.conversations[fgn].Insert(0, m);
				if (logs) print("plus ancien message");
			} else {
				User.conversations[fgn].Add(m);
				//si il s'agit du dernier message, simple ajout, sinon on ordonne la liste
				if (m.send_at < lastMessage) {
					User.conversations[fgn] = User.conversations[fgn].OrderBy(m => m.send_at).ToList();
					if (logs) print("conversation triée par date");
				} else {
					isLastMessage = true;
				}
			}
		} else {
			return; //skip réception du message si déjà en liste
		}

		//si on reçoit un message d'un user encore inconnu
		if (!User.users_infos.ContainsKey(fgn)) {
			User.users_infos[fgn] = new UserInfo() {
				id = fgn,
				nickname = "",
				sending_ratchet = new Ratchet(),
				receiving_ratchet = new Ratchet()
			};
			User.users_infos[fgn].receiving_ratchet.WARNING__rsa_private = User.WARNING___GetDefaultPrivateKey();
			if (logs) print("nous n'avons pas les infos de cet utilisateur");
			uWebSocketManager.EmitEv("request:user:info", new { id = fgn });
		}

		// si on recoit un nouveau message d'un user connu et qu'on l'a pas en focus
		else if (m.from != User.id && MM.focus_user_id != m.from && m.distributed == false) {
			TMP_Text txt = GameObject.Find("contact_" + User.users_infos[fgn].id).transform.Find("nick").GetComponent<TMP_Text>();
			txt.color = new Color(1, .63f, 0);
		}

		//si c'est pas nous qui avont envoyé le message, on précise qu'il est distribué
		if (m.from != User.id && !m.distributed) {
			uWebSocketManager.EmitEv("set:distributed", new { m.id, m.from });
		}

		//si on reçoit un message qu'on sait déjà décoder
		if (User.root_memory.ContainsKey(m.root_id)) {
			if (logs) print("capable de décrypter ce root ID !");
			m.Decrypt();
		}

		// sinon, si il s'agit d'un message d'info de l'autre user
		if (!User.root_memory.ContainsKey(m.root_id) && m.ratchet_infos != "" && m.from != User.id) {
			if (logs) print("le message contient des infos pour nous");
			// on regenère nos données d'émission avec la clé RSA fournie (pour lui fournir notre nouveau root d'émission au prochaine message)
			User.users_infos[fgn].sending_ratchet.Renew(m.sender_rsa_info, m.send_at);

			//il nous transmet également ses paramètres de ratchet d'émission avec notre clé RSA dédiée (transmise au précédent 1er message qu'on a envoyé)
			//on dédie ensuite une nouvelle clé RSA (suite de fonction SET) pour lui transférer (vu qu'on a bien récupéré son root d'émission)
			bool can_decrypt = User.users_infos[fgn].receiving_ratchet.Set(m.ratchet_infos, m.root_id, m.send_at);
			if (!can_decrypt) {
				if (logs) print("impossible de decrypter le root avec notre RSA :/");
				return;
			}

			//si on a des root de messages non rempli (avec au lieu du root l'id de l'user), 
			//c'est par ce qu'on avait pas initialisé l'utilisateur alors qu'il nous avait envoyé des messages
			foreach (var item in User.conversations[fgn].Where(x => x.root_id == m.root_id && x.decrypted == false)) {
				if (logs) print("décrypt d'un ancien message du même root ID : " + item.id);
				item.Decrypt();
			}
		}

		User.SaveData();

		if (!User.root_memory.ContainsKey(m.root_id) && !noPreviousMessageFromThosesUsers.Contains(fgn) && pending_batch == "1/1") {
			if (m.ratchet_index == 1) return;
			if (logs) print("nous n'avons pas les infos pour ce root ID, nous allons demander des anciens message. Il utilise le tick " + m.ratchet_index + ", donc il nous en faut -1");
			uWebSocketManager.EmitEv("request:messages", new { userId = fgn, lastId = m.id, limit = m.ratchet_index - 1 });
			return;
		}

		if (pending_batch.Split('/')[0] != pending_batch.Split('/')[1]) {
			if (logs) print("reception en cours, on attend le dernier message du batch");
			return;
		}

		//si on a recu juste un message seul
		if (isLastMessage && pending_batch == "1/1") {
			if (logs) print("nouveau message ! on affiche.");
			MM.Parse(m);
			return;
		}

		if (logs) print("on recharge toute la conv !");
		//sinon on recharge toute la conv
		MM.LoadConv();
	}

	public static void NoMoreMessage(string json) {
		string id = JObject.Parse(json)["id"].ToString();
		print("no more messages from " + id);
		noPreviousMessageFromThosesUsers.Add(id);
	}

	public static void MessageDistributed(string json) {
		string with = JObject.Parse(json)["with"].ToString();
		string id_message = JObject.Parse(json)["id"].ToString();

		User.conversations[with].First(m => m.id == id_message).distributed = true;
		MM.SetDistributed(with, id_message);
	}

	public static void AuthOK(string json) {
		if (!GameObject.Find("Main Camera").GetComponent<uWebSocketManager>().first) return;
		GameObject.Find("Main Camera").GetComponent<uWebSocketManager>().first = false;

		string user_id = JObject.Parse(json)["user_id"].ToString();
		User.id = user_id;
		User.LoadData();
		GameObject.Find("AuthGroup").SetActive(false);
		GameObject.Find("Canvas").GetComponent<MainClass>().SaveAuthInfos();
		uWebSocketManager.EmitEv("request:conversations");
		GameObject.Find("Canvas").GetComponent<MainClass>().logInBT.GetComponent<Button>().interactable = true;
	}

	public static void AuthError(string json) {
		string user_id = JObject.Parse(json)["message"].ToString();
		GameObject logInInfo = GameObject.Find("Canvas").GetComponent<MainClass>().logInInfo;
		GameObject.Find("Canvas").GetComponent<MainClass>().logInBT.GetComponent<Button>().interactable = true;
		logInInfo.SetActive(true);
		logInInfo.transform.Find("Txt").GetComponent<TMP_Text>().text = user_id;
	}

	public static void Err(string json) {
		string code = JObject.Parse(json)["code"].ToString();
		string message = JObject.Parse(json)["message"].ToString();
		Debug.Log("Error : " + code + " > " + message);
	}
	#endregion
}

