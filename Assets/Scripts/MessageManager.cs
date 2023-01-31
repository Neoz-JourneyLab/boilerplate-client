using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour {
	[SerializeField] TMP_InputField message_IF;
	[SerializeField] GameObject message_prefab;
	[SerializeField] Button sendBT;
	[SerializeField] GameObject message_scroll;
	public string focus_user_id = "";
	public void SendMessage() {
		if (focus_user_id == "") return;
		string rsa = User.users_infos[focus_user_id].current_public_rsa;
		int rid = User.users_infos[focus_user_id].sending_ratchet.index;
		User.users_infos[focus_user_id].sending_ratchet.index++;
		string ratchet_infos = User.users_infos[focus_user_id].sending_ratchet.root + " " +
		 User.users_infos[focus_user_id].sending_ratchet.salt + " " + rid;
		if (rid > 1) {
			ratchet_infos = "_ _ " + rid.ToString();
		}
		//var rs = new { r = Convert.ToBase64String(r), s = Convert.ToBase64String(s) };
		//ratchet_infos = Crypto.EncryptionRSA(ratchet_infos, rsa);
		uWebSocketManager.EmitEv("send:message", new { cipher = "bonjour !" + rid, ratchet_infos = ratchet_infos, to = focus_user_id });
		message_IF.text = "";
		User.SaveData("infos");
	}

	public void SetSendBT() {
		sendBT.interactable = message_IF.text != "";
	}

	public void Focus(string id) {
		focus_user_id = id;
		message_IF.interactable = true;
		LoadConv();
	}

	public void LoadConv() {
		foreach (Transform item in message_scroll.transform) {
			Destroy(item.gameObject);
		}

		if (!User.conversations.ContainsKey(focus_user_id)) return;
		foreach (var item in User.conversations[focus_user_id]) {
			GameObject m = Instantiate(message_prefab, message_scroll.transform);
			m.transform.Find("MessageBG").transform.Find("body").GetComponent<TMP_Text>().text = item.cipher;
			DateTime sendAt = DateTime.Now;
			m.transform.Find("MessageBG").transform.Find("meta").GetComponent<TMP_Text>().text = item.send_at.Hour + ":" + item.send_at.Minute;
		}
	}
}
