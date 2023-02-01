using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;
using static Tools;

/// <summary>
/// Contient tous les évenements Uws
/// </summary>
public class WsEvents : MonoBehaviour {

	#region Vars

	static int latency;
	public static readonly Dictionary<string, DateTime> pings = new Dictionary<string, DateTime>();
	public static TMP_Text serverStatus;
	static readonly MessageManager MM = GameObject.Find("Canvas").GetComponent<MessageManager>();

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
		string foreign_rsa = JObject.Parse(json)["default_public_rsa"].ToString();

		//requete forcée par reception d'un nouveau message
		if (User.users_infos.ContainsKey(id) && User.users_infos[id].nickname == "") {
			print("user nick & default rsa requested.");
			User.users_infos[id].nickname = nick;
			User.users_infos[id].default_public_rsa = foreign_rsa;

			GameObject.Find("Canvas").GetComponent<MainClass>().AddContactToList(nick, id);
			TMP_Text txt = GameObject.Find("contact_" + nick).transform.Find("nick").GetComponent<TMP_Text>();
			txt.color = new Color(1f, .63f, 0);
			User.SaveData("infos");
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
			default_public_rsa = foreign_rsa,
			isInit = true,
			sending_ratchet = new Ratchet().Init(Convert.ToBase64String(hash), foreign_rsa, "ukn", DateTime.UtcNow),
			receiving_ratchet = new Ratchet().Init(Convert.ToBase64String(hash), Crypto.Simplfy_XML_RSA(dedicaced_rsa.ToXmlString(false)), dedicaced_rsa.ToXmlString(true), DateTime.UtcNow),
		};

		MM.Focus(id);

		//il faut à présent notifier l'utilisateur distant du ROOT qu'on lui a attribué.
		//sinon, si il nous ajoute à son tour sans qu'on lui envoie un message, il nous parlera sur notre RSA par défaut
		//c'est lors de la réception de ce message qu'on pourra être sur que la com est établie
		MM.SendFirstMessage();

		GameObject.Find("Canvas").GetComponent<MainClass>().AddContactToList(nick, id);
	}

	public static void NewMessage(string json) {
		string send_at = JObject.Parse(json)["send_at"].ToString();
		bool pending_batch = JObject.Parse(json)["pending_batch"].ToString() == "True";

		Message m = JsonConvert.DeserializeObject<Message>(json);
		m.SetDate(send_at);

		if (MM.wait_for_send_next == m.id) {
			MM.CanSend();
		}

		string fgn = m.to == User.id ? m.from : m.to;
		if (!User.conversations.ContainsKey(fgn)) {
			User.conversations.Add(fgn, new List<Message>() { m });
		} else if (User.conversations[fgn].Find(mes => mes.id == m.id) == null) {
			User.conversations[fgn].Add(m);
		} else {
			return; //skip
		}

		//si on reçoit un message d'un user encore inconnu
		if (!User.users_infos.ContainsKey(fgn)) {
			User.users_infos[fgn] = new UserInfo() {
				id = fgn,
				nickname = "",
				isInit = false
			};
			uWebSocketManager.EmitEv("request:user:info", new { id = fgn });
		} 
		// si on recoit un nouveau message d'un user connu et qu'on l'a pas en focus
		else if(m.from != User.id && MM.focus_user_id != m.from && m.distributed == false) {
			TMP_Text txt = GameObject.Find("contact_" + User.users_infos[fgn].nickname).transform.Find("nick").GetComponent<TMP_Text>();
			txt.color = new Color(1, .63f, 0);
		}

		//si c'est pas nous qui avont envoyé le message, on précise qu'il est distribué
		if (m.from != User.id && !m.distributed) {
			uWebSocketManager.EmitEv("set:distributed", new { m.id, from = m.from });
		}

		//si on a pas les infos d'user (et que c'est pas nous l'emetteur), c'est qu'il nous a écrit en premier
		if (m.from != User.id && (!User.users_infos.ContainsKey(fgn) || !User.users_infos[fgn].isInit)) {
			//si il ne possède pas les infos de ratchet de de RSA c'est qu'il ne s'agit pas du premier message (on skip)
			if (!m.ratchet_infos.StartsWith("_")) {
				//on a un ratchet de réception par défaut à gaver avec ses infos fournies
				//on sait qu'il a utilisé notre clé RSA public par défault pour le premier échange (car on lui a jamais rien attribué)
				User.users_infos[fgn].receiving_ratchet = new Ratchet().Init(m.ratchet_infos.Split(' ')[0], User.GetDefaultPublicKey(), User.WARNING___GetDefaultPrivateKey(), m.send_at);

				//il nous transmet également ses paramètres de ratchet d'émission avec notre clé RSA dédiée (transmise au précédent 1er message qu'on a envoyé)
				//on dédie ensuite une nouvelle clé RSA (suite de fonction SET) pour lui transférer (vu qu'on a bien récupéré son root d'émission)
				User.users_infos[fgn].receiving_ratchet.Set(m.ratchet_infos.Split(' ')[0], m.send_at);

				//il nous fourni aussi une clé RSA dédiée pour lui répondre
				//de notre côté, on lui prépare un ratchet d'émission pour qu'il puisse nous parler sur un canal dédié (on laisse sa clé privée a ukn car inconnue)
				User.users_infos[fgn].sending_ratchet = new Ratchet().Init(Convert.ToBase64String(Crypto.GenerateRandomHash()), m.sender_rsa_info, "ukn", m.send_at);
				User.users_infos[fgn].isInit = true;

				//si on a des root de messages non rempli (avec au lieu du root l'id de l'user), 
				//c'est par ce qu'on avait pas initialisé l'utilisateur alors qu'il nous avait envoyé des messages
				print("initialisation tardive des messages pour " + fgn);
				foreach (var item in User.messageId_root.Where(v => v.Value == fgn).ToArray()) {
					User.messageId_root[item.Key] = User.users_infos[fgn].receiving_ratchet.root;
				}
			}
		}

		// sinon, si la date de réception est > à ce que l'on a
		// et qu'il s'agit d'un message étranger possédant des infos
		// prendre le plus récent (normalement le seul tant qu'on a rien envoyé) permet d'être sur le bon ratchet d'émission
		if (m.from != User.id && User.users_infos.ContainsKey(fgn) && !m.ratchet_infos.StartsWith("_")) {
			if (m.send_at > User.users_infos[fgn].sending_ratchet.valitidy) {

				// on regenère nos données d'émission avec la clé RSA fournie (pour lui fournir notre nouveau root d'émission au prochaine message)
				User.users_infos[fgn].sending_ratchet.Renew(m.send_at, m.sender_rsa_info);

				//il nous transmet également ses paramètres de ratchet d'émission avec notre clé RSA dédiée (transmise au précédent 1er message qu'on a envoyé)
				//on dédie ensuite une nouvelle clé RSA (suite de fonction SET) pour lui transférer (vu qu'on a bien récupéré son root d'émission)
				User.users_infos[fgn].receiving_ratchet.Set(m.ratchet_infos.Split(' ')[0], m.send_at);
			}
		}

		if (!User.messageId_root.ContainsKey(m.id)) {
			if (User.users_infos[fgn].isInit) {
				//si on a envoyé le message, alors son root est celui de notre ratchet d'émission
				User.messageId_root.Add(m.id, m.from == User.id ? User.users_infos[fgn].sending_ratchet.root : User.users_infos[fgn].receiving_ratchet.root);
			} else {
				User.messageId_root.Add(m.id, fgn);
			}
		}

		User.SaveData("messages infos roots");

		//si on recoit un batch de messages, on attend le dernier pour tout refresh
		if (!pending_batch) {
			//si le message est le plus récent, il s'agit d'un nouveau message seul qu'on peut décrypter et afficher.
			if (User.conversations[fgn].FirstOrDefault(me => me.send_at > m.send_at) == null) {
				m.Decrypt();
				MM.Parse(m);
			} else {
				MM.LoadConv(fgn);
			}
		}
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

