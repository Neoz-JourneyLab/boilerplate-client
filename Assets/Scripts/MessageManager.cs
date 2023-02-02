using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour {
	[SerializeField] TMP_InputField message_IF;
	[SerializeField] GameObject message_prefab;
	[SerializeField] GameObject sendBT;
	[SerializeField] GameObject message_scroll;
	[SerializeField] GameObject convoTab;
	[SerializeField] Scrollbar scrollBar;
	public string focus_user_id = "";
	public string wait_for_send_next = "";

	public GameObject confirmKeyBT;
	public GameObject confirmKeyPanel;

	private void Start() {
		InvokeRepeating(nameof(CheckScroll), 1, 1);
	}

	public void SendMessage() {
		if (wait_for_send_next != "") {
			print("wait for " + wait_for_send_next);
			return;
		}
		if (focus_user_id == "") {
			return;
		}
		if (message_IF.text == "") return;

		int rid = User.users_infos[focus_user_id].sending_ratchet.index;
		User.users_infos[focus_user_id].sending_ratchet.index++; //incrément du ratchet pour prochain message si pas reset
		string ratchet_infos = "";
		string sender_rsa_info = "";
		string root = User.users_infos[focus_user_id].sending_ratchet.root;
		string root_id = User.users_infos[focus_user_id].sending_ratchet.root_id;

		//si on envoi un message, on regarde si c'est le premier tick qu'on envoi (donc juste après un renew)
		//cela survient après la réception d'un message étranger, ou lors de la rédaction du premier message vers cet user
		if (rid == 1) {
			string rsa_public = Crypto.Regen_XML_RSA(User.users_infos[focus_user_id].sending_ratchet.rsa_public);
			//on encrypt le root avec la clé RSA publique fournie par l'autre
			string encrypted_root = Crypto.EncryptionRSA(root, rsa_public);
			ratchet_infos = encrypted_root;
			//de même, si on lui a réatribué un nouveau ratchet, on lui donne notre clé RSA pour qu'il puisse aussi nous en attribuder un
			sender_rsa_info = Crypto.Simplfy_XML_RSA(User.users_infos[focus_user_id].receiving_ratchet.rsa_public);
		}

		string id = Guid.NewGuid().ToString();
		wait_for_send_next = id;
		Message message = new Message() {
			id = id,
			plain = message_IF.text
		};
		message.Encrypt(root, rid);

		uWebSocketManager.EmitEv("send:message", new { message.cipher, ratchet_infos, to = focus_user_id, sender_rsa_info, id, ratchet_index = rid, root_id });
		message_IF.text = "";
		User.SaveData();
	}

	public void SendFirstMessage() {
		message_IF.text = "~[INIT]~";
		SendMessage();
	}

	public void SetSendBT() {
		sendBT.SetActive(message_IF.text != "" && wait_for_send_next == "");
	}

	public void Focus(string id) {
		focus_user_id = id;

		string lastId = "null";
		if (User.conversations.ContainsKey(id)) {
			lastId = User.conversations[id].OrderBy(m => m.send_at).First().id;
		}
		//if (!WsEvents.noPreviousMessageFromThosesUsers.Contains(id)) {
		//	uWebSocketManager.EmitEv("request:messages", new { userId = id, lastId });
		//}

		if (id == "") {
			convoTab.SetActive(false);
			return;
		}
		convoTab.SetActive(true);
		message_IF.interactable = true;
		if (User.conversations.ContainsKey(id) && User.conversations[id].Count > 0) {
			User.conversations[id] = User.conversations[id].OrderBy(m => m.send_at).ToList();
		}
		LoadConv();
		confirmKeyBT.SetActive(true);
	}


	string key = "";
	public void ConfirmKey() {
		confirmKeyPanel.SetActive(!confirmKeyPanel.activeInHierarchy);
		confirmKeyPanel.GetComponentInChildren<TMP_Text>().text = Languages.Get("SHARE_INFO");// "SHARE THIS WITH YOUR CONTACT<br>TO BE SURE THAT YOU USE THE GOOD KEY<br><#FF0000>IF NOT THE SAME, THERE IS A BUG<br>OR A MAN IN THE MIDDLE ATTACK AND NOTHING IS SAFE<br>";
																																													//si nous avons envoyé le dernier message, alors la clé RSA synchronisée est celle de reception
																																													//en effet, la future clé de réception distante a déjà été renouvelée par l'utilisateur distant dès la réception de notre message
		if (User.conversations[focus_user_id].Last().from == User.id) {
			string rsa = User.users_infos[focus_user_id].receiving_ratchet.rsa_public;
			string rsa_split = "";
			int count = 1;
			foreach (char c in rsa) {
				rsa_split += c;
				if (count > 1 && count % 10 == 0) rsa_split += " ";
				if (count > 1 && count % 40 == 0) rsa_split += "\n";
				count++;
			}
			key = rsa_split;
			confirmKeyPanel.GetComponentInChildren<TMP_Text>().text += Languages.Get("RECIEVE_INFO")/*"<br><#00FFFF><u>YOU WILL RECIEVE NEXT RATCHET INFO WITH YOUR KEY :</u><br><br>"*/ + rsa_split;
		}
		//a l'inverse, si nous ne somme pas le dernier à avoir envoyé un message, nous avons les infos pour l'émission à l'utilisateur distant
		else {
			string rsa = User.users_infos[focus_user_id].sending_ratchet.rsa_public;
			string rsa_split = "";
			int count = 1;
			foreach (char c in rsa) {
				rsa_split += c;
				if (count > 1 && count % 10 == 0) rsa_split += " ";
				if (count > 1 && count % 40 == 0) rsa_split += "\n";
				count++;
			}
			key = rsa_split;
			confirmKeyPanel.GetComponentInChildren<TMP_Text>().text += Languages.Get("SEND_INFO") /* "<br><#FFFF00><u>YOU WILL SEND NEXT RATCHET INFO ON HIS KEY :</u><br><br>" */ + rsa_split;
		}
	}

	public void CopyKey() {
		UniClipboard.SetText(key.ToUpper());
	}

	public void Clear() {
		foreach (Transform item in message_scroll.transform) {
			Destroy(item.gameObject);
		}
		Focus("");
	}

	//prevent spam / wrong ratchet clicks
	public void CanSend() {
		wait_for_send_next = "";
		SetSendBT();
	}

	string lastLoad = "";
	bool isScrolling = false;
	private void CheckScroll() {
		if(isScrolling && scrollBar.value >= 1f) {
			LoadMore();
			isScrolling = false;
			return;
		}
		if (scrollBar.value >= 1f) isScrolling = true;
	}

	public void LoadMore() {
		if (focus_user_id == "") return;
		string lastId = User.conversations[focus_user_id].First().id;
		if (lastLoad == lastId) {
			print("loast load same ID : " + lastLoad);
			return;
		}
		lastLoad = lastId;
		if (WsEvents.noPreviousMessageFromThosesUsers.Contains(focus_user_id)) return;
		uWebSocketManager.EmitEv("request:messages", new { userId = focus_user_id, lastId, limit = 10 });
	}

	public void LoadConv(string idFocus = "") {
		if (idFocus == "") idFocus = focus_user_id;
		if (idFocus != focus_user_id) return;
		if (!User.conversations.ContainsKey(focus_user_id) || focus_user_id == "") return;

		foreach (Transform item in message_scroll.transform) {
			Destroy(item.gameObject);
		}

		DateTime last = DateTime.MinValue;
		foreach (var message in User.conversations[focus_user_id]) {
			if (message.plain == "") continue;

			var delta = (message.send_at - last).TotalHours;
			last = message.send_at;
			if (delta > 12) {
				GameObject m = Instantiate(message_prefab, message_scroll.transform);
				m.transform.Find("MessageBG").transform.Find("body").GetComponent<TMP_Text>().text = message.send_at.ToLongDateString();
				Destroy(m.transform.Find("MessageBG").transform.Find("meta").gameObject);
				m.transform.Find("MessageBG").GetComponent<Image>().color = new Color(0.15f, 0.20f, 0.25f);
				m.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(250, 250, 0, 0);
				m.name = "DATE_SEPARATOR" + last.ToLongDateString();
			}
			SetUpMessage(message);
		}

		LayoutRebuilder.ForceRebuildLayoutImmediate(message_scroll.GetComponent<RectTransform>());
		Canvas.ForceUpdateCanvases();
	}

	public void SetDistributed(string with, string message_id) {
		if (focus_user_id != with) return;
		GameObject.Find(message_id).transform.Find("MessageBG").transform.Find("meta").GetComponent<TMP_Text>().color = new Color(0, 0.8f, 1f);
	}

	public GameObject SetUpMessage(Message message) {
		GameObject m = Instantiate(message_prefab, message_scroll.transform);
		m.name = message.id;
		m.transform.Find("MessageBG").transform.Find("body").GetComponent<TMP_Text>().text = message.plain;
		m.transform.Find("MessageBG").transform.Find("meta").GetComponent<TMP_Text>().text = message.send_at.Hour.ToString("00") + ":" + message.send_at.Minute.ToString("00");
		if (message.from == User.id) {
			m.transform.Find("MessageBG").transform.Find("meta").GetComponent<TMP_Text>().color = message.distributed ? new Color(0, 0.8f, 1f) : Color.gray;
			m.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
			m.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(200, 0, 0, 0);
			m.transform.Find("MessageBG").GetComponent<Image>().color = new Color(0.19f, 0.32f, 0.53f);
		} else {
			m.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
			m.transform.Find("MessageBG").transform.Find("meta").GetComponent<TMP_Text>().color = Color.white;
			m.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(0, 200, 0, 0);
			m.transform.Find("MessageBG").GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
		}
		return m;
	}

	public void Parse(Message m) {
		if (!User.conversations.ContainsKey(focus_user_id) || focus_user_id != (m.from == User.id ? m.to : m.from)) {
			return;
		}

		if (m.plain == "~[INIT]~") {
			m.plain = Languages.Get("Ceci est le début de vos conversations avec " + User.users_infos[focus_user_id].nickname);
		}

		GameObject go = SetUpMessage(m);

		//dernier sibling si on Parse()

		LayoutRebuilder.ForceRebuildLayoutImmediate(message_scroll.GetComponent<RectTransform>());
		Canvas.ForceUpdateCanvases();
	}
}