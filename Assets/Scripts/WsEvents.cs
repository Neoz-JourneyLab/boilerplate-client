using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Tools;

/// <summary>
/// Contains all Uws events
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

	/// <summary>
	/// get server status text on top left corner
	/// </summary>
	public static TMP_Text GetServerStatusTxt() {
		if (serverStatus == null) {
			serverStatus = GameObject.Find("server infos").GetComponent<TMP_Text>();
		}
		return serverStatus;
	}

	/// <summary>
	/// response from ping, actualize latency and precise server time
	/// </summary>
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

	/// <summary>
	/// information about an user : ID and nickname
	/// perform an initialisation of ratchets if needed
	/// </summary>
	public static void UserInfos(string json) {
		if (json.StartsWith("{\"err\"")) {
			string err = JObject.Parse(json)["err"].ToString();
			GameObject.Find("Canvas").GetComponent<MainClass>().Prompt(err);
			return;
		}
		string id = JObject.Parse(json)["id"].ToString();
		string nick = JObject.Parse(json)["nickname"].ToString();
		string foreign_default_rsa = JObject.Parse(json)["default_public_rsa"].ToString();

		//[CASE 1] request forced by reception of a first message
		if (User.users_infos.ContainsKey(id) && User.users_infos[id].nickname == "") {
			User.users_infos[id].nickname = nick;
			User.users_infos[id].receiving_ratchet.WARNING__rsa_private = User.WARNING___GetDefaultPrivateKey();

			GameObject.Find("Canvas").GetComponent<MainClass>().AddContactToList(nick, id);
			TMP_Text txt = GameObject.Find("contact_" + id).transform.Find("nick").GetComponent<TMP_Text>();
			txt.color = new Color(1f, .63f, 0);
			User.SaveData();
			return;
		}

		//[CASE 2] voluntary asked about user information, we create a sending ratchet
		//we assign him a dedicated RSA to send us his sending roots
		RSACryptoServiceProvider dedicaced_rsa = new RSACryptoServiceProvider();
		string public_rsa = Crypto.Simplfy_XML_RSA(dedicaced_rsa.ToXmlString(false));
		string private_rsa = dedicaced_rsa.ToXmlString(true);

		User.users_infos[id] = new UserInfo() {
			id = id,
			nickname = nick,
			sending_ratchet = new Ratchet().Init(Crypto.Simplfy_XML_RSA(foreign_default_rsa), "ukn"),
			receiving_ratchet = new Ratchet().Init(public_rsa, private_rsa),
		};

		//store root information
		string root_id = User.users_infos[id].sending_ratchet.root_id;
		string root = User.users_infos[id].sending_ratchet.root;
		User.root_memory.Add(root_id, root);

		MM.Focus(id);

		//we must now notify the remote user of the ROOT assigned to him.
		//it is when this message is received that we can be sure that the communication is established
		MM.SendFirstMessage();

		GameObject.Find("Canvas").GetComponent<MainClass>().AddContactToList(nick, id);
	}

	static bool logs = false; //enable for debugging logs on message receptions

	/// <summary>
	/// occurs when receiving a new message
	/// update ratchets infos and request needed users informations
	/// </summary>
	public static void NewMessage(string json) {
		//convert js-date to c# format
		var settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None };
		JObject jObj = JsonConvert.DeserializeObject<JObject>(json, settings);
		string send_at = jObj.SelectToken("send_at").ToString();
		Message m = JsonConvert.DeserializeObject<Message>(json);
		m.SetDate(send_at);

		//info about message count that server is currently sending to us
		string pending_batch = JObject.Parse(json)["pending_batch"].ToString();

		//allow back user to send message 
		if (MM.wait_for_send_next == m.id) MM.CanSend();

		//id of foreign user related to this message
		string fgn = m.to == User.id ? m.from : m.to;

		//is that message the last from that conversation ?
		bool isLastMessage = false;

		if (logs) print("réception du message " + m.id + ", " + pending_batch + ", root ID " + m.root_id);

		//here, it is the first message received (thus the last one sent)
		if (!User.conversations.ContainsKey(fgn)) {
			User.conversations.Add(fgn, new List<Message>() { m });
			isLastMessage = true;
		} 
		//conversation with user already exists
		else {
			//if it's a message not already recieved
			if (User.conversations[fgn].Find(mes => mes.id == m.id) != null) return;

			DateTime lastMessage = User.conversations[fgn].Last().send_at;
			DateTime firstMessage = User.conversations[fgn].First().send_at;

			//or if it's a newer post than all, just add it to the top
			if (firstMessage > m.send_at) {
				User.conversations[fgn].Insert(0, m);
				if (logs) print("plus ancien message");
			} else {
				User.conversations[fgn].Add(m);
				//if it is the last message, simple addition, otherwise we order the list
				if (m.send_at < lastMessage) {
					User.conversations[fgn] = User.conversations[fgn].OrderBy(m => m.send_at).ToList();
					if (logs) print("conversation triée par date");
				} else {
					isLastMessage = true;
				}
			}
		}

		//if we receive a message from a yet unknown user
		if (!User.users_infos.ContainsKey(fgn)) {
			User.users_infos[fgn] = new UserInfo() {
				id = fgn,
				nickname = "",
				sending_ratchet = new Ratchet(),
				receiving_ratchet = new Ratchet()
			};
			//a new user used our public DEFAULT rsa key
			string private_rsa_key = User.WARNING___GetDefaultPrivateKey();
			User.users_infos[fgn].receiving_ratchet.WARNING__rsa_private = private_rsa_key;
			if (logs) print("nous n'avons pas les infos de cet utilisateur");
			uWebSocketManager.EmitEv("request:user:info", new {
				id = fgn
			});
		}

		//if we sent that message
		if (m.from == User.id) {
			User.users_infos[fgn].sending_ratchet.index++; //ratchet increment for next message we will send
		}

		//if we receive a new message from a known user and we don't have it in focus
		else if (m.from != User.id && MM.focus_user_id != m.from && m.distributed == false) {
			TMP_Text txt = GameObject.Find("contact_" + User.users_infos[fgn].id).transform.Find("nick").GetComponent<TMP_Text>();
			txt.color = new Color(1, .63f, 0);
		}

		//if it is not us who sent the message, we specify that it is distributed
		if (m.from != User.id && !m.distributed) {
			uWebSocketManager.EmitEv("set:distributed", new { m.id, m.from });
		}

		//if we receive a message that we already know how to decode
		if (User.root_memory.ContainsKey(m.root_id)) {
			if (logs) print("capable de décrypter ce root ID !");
			m.Decrypt();
		}

		//otherwise, if it is an info message from the other user
		if (!User.root_memory.ContainsKey(m.root_id) && m.ratchet_infos != "" && m.from != User.id) {
			if (logs) print("le message contient des infos pour nous");
			//we regenerate our sending data with the RSA key provided (to provide it with our new issue root on the next message)
			User.users_infos[fgn].sending_ratchet.Renew(m.sender_rsa_info, m.send_at);

			//it also sends us its send ratchet parameters with our dedicated RSA key (transmitted to the previous 1st message we sent)
			//we then dedicate a new RSA key (suite of SET function) to transfer it (since we have recovered its sending root)
			bool can_decrypt = User.users_infos[fgn].receiving_ratchet.Set(m.ratchet_infos, m.root_id, m.send_at);
			if (!can_decrypt) {
				if (logs) print("impossible de decrypter le root avec notre RSA :/");
				return;
			}

			//if we have unfilled message roots (with the user's id instead of the root),
			//it's because we didn't initialize the user when he sent us messages
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

		//if we just received a single message
		if (isLastMessage && pending_batch == "1/1") {
			if (logs) print("nouveau message ! on affiche.");
			MM.Parse(m);
			return;
		}

		if (logs) print("on recharge toute la conv !");
		//otherwise we reload the whole conv
		MM.LoadConv();
	}

	/// <summary>
	/// server inform that we had all old messages
	/// </summary>
	public static void NoMoreMessage(string json) {
		string id = JObject.Parse(json)["id"].ToString();
		print("no more messages from " + id);
		noPreviousMessageFromThosesUsers.Add(id);
	}

	/// <summary>
	/// server inform that one of our message was distributed
	/// </summary>
	public static void MessageDistributed(string json) {
		string with = JObject.Parse(json)["with"].ToString();
		string id_message = JObject.Parse(json)["id"].ToString();

		User.conversations[with].First(m => m.id == id_message).distributed = true;
		MM.SetDistributed(with, id_message);
	}

	/// <summary>
	/// server grand access so we can continue log in by requesting user informations
	/// </summary>
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

	/// <summary>
	/// server tell us that auth was wrong
	/// </summary>
	/// <param name="json"></param>
	public static void AuthError(string json) {
		string user_id = JObject.Parse(json)["message"].ToString();
		GameObject logInInfo = GameObject.Find("Canvas").GetComponent<MainClass>().logInInfo;
		GameObject.Find("Canvas").GetComponent<MainClass>().logInBT.GetComponent<Button>().interactable = true;
		logInInfo.SetActive(true);
		logInInfo.transform.Find("Txt").GetComponent<TMP_Text>().text = user_id;
	}

	/// <summary>
	/// general error from server
	/// </summary>
	public static void Err(string json) {
		string code = JObject.Parse(json)["code"].ToString();
		string message = JObject.Parse(json)["message"].ToString();
		GameObject.Find("Canvas").GetComponent<MainClass>().Prompt("<#FF3000>" + Languages.Get(code) + "\n<#FFF205>" + message);
	}
	#endregion
}

