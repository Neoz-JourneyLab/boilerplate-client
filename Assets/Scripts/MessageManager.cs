using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour {
	[SerializeField] TMP_InputField message_IF;
	[SerializeField] GameObject message_prefab;
	[SerializeField] Button sendBT;
	[SerializeField] GameObject message_scroll;
	public string focus_user_id = "";
	public string wait_for_send_next = "";

	private void Awake() {
		var s = Crypto.GenerateRandomHash();
	}

	public void SendMessage() {
		if (wait_for_send_next != "") return;
		if (focus_user_id == "") return;

		int rid = User.users_infos[focus_user_id].sending_ratchet.index;
		User.users_infos[focus_user_id].sending_ratchet.index++; //incrément du ratchet pour prochain message si pas reset
		string ratchet_infos = "_ " + rid.ToString();
		string sender_rsa_info = "";


		//si on envoi un message, on regarde si c'est le premier tick qu'on envoi (donc juste après un renew)
		//cela survient après la réception d'un message étranger, ou lors de la rédaction du premier message vers cet user
		if (rid == 1) {
			ratchet_infos = User.users_infos[focus_user_id].sending_ratchet.root + " " + rid;
			//de même, si on lui a réatribué un nouveau ratchet, on lui donne notre clé RSA pour qu'il puisse aussi nous en attribuder un
			sender_rsa_info = Crypto.Simplfy_XML_RSA(User.users_infos[focus_user_id].receiving_ratchet.rsa_public);
		}

		string id = Guid.NewGuid().ToString();
		wait_for_send_next = id;
		Message message = new Message() {
			id = id,
			plain = message_IF.text
		};
		message.Encrypt(User.users_infos[focus_user_id].sending_ratchet.root, rid);

		uWebSocketManager.EmitEv("send:message", new { message.cipher, ratchet_infos, to = focus_user_id, sender_rsa_info, id });
		message_IF.text = "";
		User.SaveData("infos");
	}

	public void SetSendBT() {
		sendBT.interactable = message_IF.text != "" && wait_for_send_next == "";
	}

	public void Focus(string id) {
		focus_user_id = id;
		message_IF.interactable = true;
		LoadConv();
	}

	public void Clear() {
		focus_user_id = "";
		foreach (Transform item in message_scroll.transform) {
			Destroy(item.gameObject);
		}
	}

	//prevent spam / wrong ratchet clicks
	public void CanSend() {
		wait_for_send_next = "";
		SetSendBT();
	}

	public void LoadConv() {
		if (!User.conversations.ContainsKey(focus_user_id) || focus_user_id == "") return;

		foreach (Transform item in message_scroll.transform) {
			Destroy(item.gameObject);
		}
		User.conversations[focus_user_id] = User.conversations[focus_user_id].OrderBy(m => m.send_at).ToList();

		if (User.conversations[focus_user_id].Count > 1) {
			foreach (var mes in User.conversations[focus_user_id].ToArray()) {
				mes.Decrypt();
			}
		}

		foreach (var item in User.conversations[focus_user_id]) {
			GameObject m = Instantiate(message_prefab, message_scroll.transform);
			m.transform.Find("MessageBG").transform.Find("body").GetComponent<TMP_Text>().text = item.plain;
			DateTime sendAt = DateTime.Now;
			m.transform.Find("MessageBG").transform.Find("meta").GetComponent<TMP_Text>().text = item.send_at.Hour + ":" + item.send_at.Minute;
		}
	}

	public void Parse(Message m) {
		if (!User.conversations.ContainsKey(focus_user_id) || focus_user_id != (m.from == User.id ? m.to : m.from)) {
			return;
		}

		GameObject go = Instantiate(message_prefab, message_scroll.transform);
		go.transform.Find("MessageBG").transform.Find("body").GetComponent<TMP_Text>().text = m.plain;
		DateTime sendAt = DateTime.Now;
		go.transform.Find("MessageBG").transform.Find("meta").GetComponent<TMP_Text>().text = m.send_at.Hour + ":" + m.send_at.Minute;
		int indx = 0;

		for (int i = 0; i < User.conversations[focus_user_id].ToArray().Length; i++) {
			if (m.id == User.conversations[focus_user_id][i].id) continue;
			if (m.send_at > User.conversations[focus_user_id][i].send_at) {
				indx = i + 1;
			}
		}

		go.transform.SetSiblingIndex(indx);
		if (indx != User.conversations[focus_user_id].ToArray().Length - 1) {
			User.conversations[focus_user_id] = User.conversations[focus_user_id].OrderBy(m => m.send_at).ToList();
		}
	}
}