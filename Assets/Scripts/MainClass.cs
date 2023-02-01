using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Crypto;

public class MainClass : MonoBehaviour {
	[SerializeField] TMP_InputField nickname_IF;
	[SerializeField] TMP_InputField password_IF;
	[SerializeField] GameObject authGroup;
	public GameObject logInBT;
	public GameObject logInInfo;

	[SerializeField] GameObject contact_prefab;
	[SerializeField] GameObject contact_scroll;
	[SerializeField] Button addContactBT;
	[SerializeField] TMP_InputField new_contact;

	[SerializeField] TMP_Text console;

	public bool mem_nick = true;
	public bool mem_pass = false;
	[SerializeField] TMP_Text mem_nick_txt;
	[SerializeField] TMP_Text mem_pass_txt;

	int min_len_pass = 5;
	int min_len_nick = 4;

	private void Awake() {
		Languages.Init(Application.systemLanguage);
		authGroup.SetActive(true);
	}

	void Start() {
		Application.targetFrameRate = 90;
		if (File.Exists(Application.streamingAssetsPath + "/" + "last_auths_infos.txt")) {
			string autolog = File.ReadAllText(Application.streamingAssetsPath + "/" + "last_auths_infos.txt");
			var key = Crypto.Hash(Encoding.UTF8.GetBytes(GetMac()));
			var IV = Crypto.KDF(Encoding.UTF8.GetBytes(GetMac()), Encoding.UTF8.GetBytes(GetMac()), 128);
			autolog = Crypto.DecryptAES(Convert.FromBase64String(autolog), key, IV);

			AutoLog al = JsonConvert.DeserializeObject<AutoLog>(autolog);
			if (al.nick != "") {
				nickname_IF.text = al.nick;
				mem_nick = false;
				ChangeMemNick();
			}
			if (al.pass != "") {
				password_IF.text = al.pass;
				mem_pass = false;
				ChangeMemPass();
				//will auto log in
				logInBT.GetComponent<Button>().interactable = false;
			}
		}
	}

	public void SaveAuthInfos() {
		string json = JsonConvert.SerializeObject(new AutoLog() { nick = mem_nick ? nickname_IF.text : "", pass = mem_pass ? password_IF.text : "" }, Formatting.Indented);
		print("MAC : " + GetMac());
		var key = Crypto.Hash(Encoding.UTF8.GetBytes(GetMac()));
		var IV = Crypto.KDF(Encoding.UTF8.GetBytes(GetMac()), Encoding.UTF8.GetBytes(GetMac()), 128);
		string encrypt = Convert.ToBase64String(Crypto.EncryptAES(json, key, IV));
		File.WriteAllText(Application.streamingAssetsPath + "/" + "last_auths_infos.txt", encrypt);
	}

	public void SetPassKdf() {
		byte[] hash = Crypto.Hash(Encoding.UTF8.GetBytes(password_IF.text));
		User.pass_IV = Crypto.KDF(hash, hash, 32);
		User.pass_kdf = Crypto.KDF(User.pass_IV, hash, 64);
	}

	public string GetMac() {
		var macAddr =
		(
				from nic in NetworkInterface.GetAllNetworkInterfaces()
				where nic.OperationalStatus == OperationalStatus.Up
				select nic.GetPhysicalAddress().ToString()
		).FirstOrDefault();
		if(macAddr == null || macAddr == "") {
			macAddr = "NO MAC ADRESS";
		}
		return macAddr;
	}

	class AutoLog {
		public string nick;
		public string pass;
	}

	public void Auth() {
		if (nickname_IF.text.Length < min_len_nick || password_IF.text.Length < min_len_pass) return;
		logInBT.GetComponent<Button>().interactable = false;
		User.nickname = nickname_IF.text;
		User.InitPrivateKey();
		uWebSocketManager.EmitEv("auth", new { nickname = nickname_IF.text, password = password_IF.text, public_rsa = Crypto.Simplfy_XML_RSA(User.GetDefaultPublicKey()) });
	}

	public void AddContactToList(string name, string id) {
		GameObject go = Instantiate(contact_prefab, contact_scroll.transform);
		go.name = "contact_" + name;
		go.transform.Find("nick").GetComponent<TMP_Text>().text = name;
		go.GetComponent<ContactPrefab>().id = id;
	}

	public void MakeAddContactEnbled() {
		addContactBT.interactable = new_contact.text.Length > 3;
	}

	public void MakeLogInVisible() {
		SetPassKdf();

		logInBT.SetActive(password_IF.text.Length >= min_len_pass && nickname_IF.text.Length >= min_len_nick);
		if (!logInBT.activeSelf) {
			logInInfo.SetActive(true);
			logInInfo.transform.Find("Txt").GetComponent<TMP_Text>().text = Languages.Get("min_req_log").Replace("#X", min_len_nick.ToString()).Replace("#Y", min_len_pass.ToString());
		} else {
			logInInfo.SetActive(false);
		}
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

	public void ChangeMemNick() {
		mem_nick = !mem_nick;
		mem_nick_txt.color = mem_nick ? new Color(0.56f, 0.88f, 1) : new Color(0.35f, 0.35f, 0.35f);
		mem_nick_txt.transform.parent.Find("Image").gameObject.SetActive(mem_nick);
	}
	public void ChangeMemPass() {
		mem_pass = !mem_pass;
		mem_pass_txt.color = mem_pass ? new Color(1f, 0.46f, 0.35f) : new Color(0.35f, 0.35f, 0.35f);
		mem_pass_txt.transform.parent.Find("Image").gameObject.SetActive(mem_pass);
	}
}
