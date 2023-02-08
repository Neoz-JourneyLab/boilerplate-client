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

	string key = "";

	string lastLoad = "";
	bool isScrolling = false;

	private void Start() {
		//check every second if user scroll conversation to load old messages
		InvokeRepeating(nameof(CheckScroll), 1, 1);
	}

	/// <summary>
	/// send a new message to the focused user
	/// </summary>
	public void SendMessage() {
		//to disable spam, wait response from server of previous sent message
		if (wait_for_send_next != "") return;
		if (focus_user_id == "") return;
		if (message_IF.text == "") return;

		int rid = User.users_infos[focus_user_id].sending_ratchet.index;
		string ratchet_infos = "";
		string sender_rsa_info = "";
		string root = User.users_infos[focus_user_id].sending_ratchet.root;
		string root_id = User.users_infos[focus_user_id].sending_ratchet.root_id;

		//if we send a message, we check if it's the first tick we send (so just after a renew)
		//this happens after receiving a foreign message, or when writing the first message to this user
		if (rid == 1) {
			string rsa_public = Crypto.Regen_XML_RSA(User.users_infos[focus_user_id].sending_ratchet.rsa_public);
			//we encrypt the root with the public RSA key provided by the other
			ratchet_infos = Crypto.EncryptionRSA(root, rsa_public);
			//likewise, if we assigned a new ratchet, we give it our RSA key so that it can also assign us one
			sender_rsa_info = Crypto.Simplfy_XML_RSA(User.users_infos[focus_user_id].receiving_ratchet.rsa_public);
		}

		string id = Guid.NewGuid().ToString(); //random UUID for the message
		wait_for_send_next = id; //store this to prevent spamming
		Message message = new Message() {
			id = id,
			root_id = root_id,
			plain = message_IF.text
		};
		message.Encrypt(root, rid); //plain is cleared during this method, and cipher is created

		uWebSocketManager.EmitEv("send:message", new { message.cipher, ratchet_infos, to = focus_user_id, sender_rsa_info, id, ratchet_index = rid, root_id });
		message_IF.text = "";
	}

	/// <summary>
	/// Send first message to user, initialisating our sending ratchet
	/// </summary>
	public void SendFirstMessage() {
		message_IF.text = "~[INIT]~";
		SendMessage();
	}

	/// <summary>
	/// set send button if ready
	/// </summary>
	public void SetSendBT() {
		sendBT.GetComponent<Button>().interactable = (message_IF.text != "" && wait_for_send_next == "");
	}

	/// <summary>
	/// focus a User and load it's conversation, or close convo tab
	/// </summary>
	/// <param name="id">user if to focus</param>
	public void Focus(string id) {
		focus_user_id = id;

		string lastId = "null";
		if (User.conversations.ContainsKey(id)) {
			lastId = User.conversations[id].OrderBy(m => m.send_at).First().id;
		}

		if (id == "") {
			convoTab.SetActive(false);
			return;
		}

		convoTab.SetActive(true);
		message_IF.interactable = true;

		LoadConv();
		confirmKeyBT.SetActive(true);
	}

	/// <summary>
	/// display next public RSA key that will be used to prevent man in the middle
	/// </summary>
	public void ConfirmKey() {
		confirmKeyPanel.SetActive(!confirmKeyPanel.activeInHierarchy);
		confirmKeyPanel.GetComponentInChildren<TMP_Text>().text = Languages.Get("SHARE_INFO");
		//next RSA is on our sending ratchet if we sent last message
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
			confirmKeyPanel.GetComponentInChildren<TMP_Text>().text += Languages.Get("RECIEVE_INFO") + rsa_split;
		}
		//next RSA is on our receiving ratchet if we receive last message
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
			confirmKeyPanel.GetComponentInChildren<TMP_Text>().text += Languages.Get("SEND_INFO") + rsa_split;
		}
	}

	/// <summary>
	/// copy public key to clipboard
	/// </summary>
	public void CopyKey() {
		UniClipboard.SetText(key.ToUpper());
	}

	/// <summary>
	/// destroy all message prefabs
	/// </summary>
	public void Clear() {
		foreach (Transform item in message_scroll.transform) {
			Destroy(item.gameObject);
		}
		Focus("");
	}

	/// <summary>
	/// confirm that last send message was correctly sent
	/// </summary>
	public void CanSend() {
		wait_for_send_next = "";
		SetSendBT();
	}

	/// <summary>
	/// check if user is scrolling convo to load previous messages
	/// </summary>
	private void CheckScroll() {
		if (isScrolling && scrollBar.value >= 1f) {
			LoadMore();
			isScrolling = false;
			return;
		}
		if (scrollBar.value >= 1f) isScrolling = true;
	}

	/// <summary>
	/// load previous messages if available
	/// </summary>
	public void LoadMore() {
		if (focus_user_id == "") return;
		string lastId = User.conversations[focus_user_id].First().id;
		if (lastLoad == lastId) { //if try to load from same oldest message, cancel
			return;
		}
		lastLoad = lastId;
		if (WsEvents.noPreviousMessageFromThosesUsers.Contains(focus_user_id)) return;
		uWebSocketManager.EmitEv("request:messages", new { userId = focus_user_id, lastId, limit = 10 });
	}

	/// <summary>
	/// parse a complete conversation
	/// </summary>
	public void LoadConv(string idFocus = "") {
		if (idFocus == "") idFocus = focus_user_id;
		if (idFocus != focus_user_id) return;
		if (!User.conversations.ContainsKey(focus_user_id) || focus_user_id == "") return;

		//clear scroll view
		foreach (Transform item in message_scroll.transform) {
			Destroy(item.gameObject);
		}

		DateTime last = DateTime.MinValue;
		//parse all messages
		foreach (var message in User.conversations[focus_user_id]) {
			if (message.plain == "") continue;

			var delta = (message.send_at - last).TotalHours;
			last = message.send_at;
			//add a day/date separator if message are spaced in time
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

	/// <summary>
	///change the color of time of a message to notify user that his message was delivered
	/// </summary>
	public void SetDistributed(string with, string message_id) {
		if (focus_user_id != with) return;
		GameObject.Find(message_id).transform.Find("MessageBG").transform.Find("meta").GetComponent<TMP_Text>().color = new Color(0, 0.8f, 1f);
	}

	/// <summary>
	/// Set up a message prefab GO
	/// </summary>
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

	/// <summary>
	/// Parse a single message to the focused conversation
	/// </summary>
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