using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Crypto;

public class MainClass : MonoBehaviour {
	[SerializeField] TMP_InputField nickname_IF;
	[SerializeField] TMP_InputField password_IF;
	[SerializeField] GameObject authGroup;

	[SerializeField] GameObject contact_prefab;
	[SerializeField] GameObject contact_scroll;
	[SerializeField] Button addContactBT;
	[SerializeField] TMP_InputField new_contact;

	[SerializeField] TMP_Text console;

	private void Awake() {
		authGroup.SetActive(true);
	}

	void Start() {
		Application.targetFrameRate = 90;
		nickname_IF.text = "Boby";
		password_IF.text = "AZErty";
	}

	public void Auth() {
		User.nickname = nickname_IF.text;
		User.InitPrivateKey();
		uWebSocketManager.EmitEv("auth", new { nickname = nickname_IF.text, password = password_IF.text, public_rsa = User.GetDefaultPublicKey() });
	}

	public void AddContactToList(string name, string id) {
		GameObject go = Instantiate(contact_prefab, contact_scroll.transform);
		go.name = "contact_" + name;
		go.transform.Find("nick").GetComponent<TMP_Text>().text = name;
		go.transform.Find("last_message").GetComponent<TMP_Text>().text = "";
		go.GetComponent<ContactPrefab>().id = id;
	}

	public void MakeAddContactEnbled() {
		addContactBT.interactable = new_contact.text.Length > 3;
	}

	public void AddContact() {
		uWebSocketManager.EmitEv("request:user:info", new { nickname = new_contact.text });
		new_contact.text = "";
	}


	IEnumerator ClearConsole() {
		yield return new WaitForSeconds(3);
		while (console.color.a > 0) {
			console.color = new Color(console.color.r, console.color.g, console.color.b, console.color.a - 0.1f);
			yield return new WaitForSeconds(0.05f);
		}
		console.text = "";
	}

	public void Prompt(string log) {
		StopCoroutine(nameof(ClearConsole));
		console.text = log;
		console.color = new Color(1, 0, 0, 1);
		StartCoroutine(nameof(ClearConsole));
	}
}
