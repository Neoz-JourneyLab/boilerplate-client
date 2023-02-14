using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Crypto;

public class MainClass : MonoBehaviour {
	[SerializeField] TMP_InputField nickname_IF;
	[SerializeField] TMP_InputField password_IF;
	[SerializeField] TMP_InputField realm_IF;
	public GameObject authGroup;
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
	readonly int min_len_pass = 5;
	readonly int min_len_nick = 4;
	readonly string last_auth_info_file = Application.streamingAssetsPath + "/" + "last_auths_infos.txt";

	/// <summary>
	/// Save user information that was not saved when app in quitted
	/// </summary>
	void OnApplicationQuit() {
		User.SaveData(true);
		GameObject.FindGameObjectWithTag("AppManager").GetComponent<uWebSocketManager>().Close();
	}

	private void Awake() {
		string realm_path = Application.streamingAssetsPath + "/realm.txt";
		if (!File.Exists(realm_path)) {
			File.WriteAllText(realm_path, "ws://localhost:9997/"); //default local ip
		}
		//load language manager and prompt auth screen
		realm_IF.text = File.ReadAllText(realm_path);
		Languages.Init(Application.systemLanguage);
		authGroup.SetActive(true);
	}

	void Start() {
		Application.targetFrameRate = 90;
		//decode credentials if available on local storage
		if (!File.Exists(last_auth_info_file)) return;

		try {
			string autolog = File.ReadAllText(last_auth_info_file);
			//key is mac adress
			var key = Hash(Encoding.UTF8.GetBytes(GetMac()));
			var IV = KDF(Encoding.UTF8.GetBytes(GetMac()), Encoding.UTF8.GetBytes(GetMac()), 16);

			//json infos are read from decrypted file
			autolog = DecryptAES(Convert.FromBase64String(autolog), key, IV);
			AutoLog al = JsonConvert.DeserializeObject<AutoLog>(autolog);
			if (al.nick != "") {
				nickname_IF.text = al.nick;
				mem_nick = false;
				ChangeMemNick(); //enable memorize nick
			}
			if (al.pass != "") {
				password_IF.text = al.pass;
				mem_pass = false;
				ChangeMemPass(); //enable memorize pass

				//will auto log in : disable log in button
				logInBT.GetComponent<Button>().interactable = false;
			}
		} catch(Exception ex) {
			print("cannot read creds info : " + ex.Message);
			File.Delete(last_auth_info_file);
		}
	}

	/// <summary>
	/// Set input for log in disabled or enabled
	/// </summary>
	public void SetInputsLogIng(bool state) {
		nickname_IF.interactable = state;
		password_IF.interactable = state;
	}

	/// <summary>
	/// save credentials if enabled, encrypt with mac adress
	/// </summary>
	public void SaveAuthInfos() {
		string json = JsonConvert.SerializeObject(new AutoLog() {
			nick = mem_nick ? nickname_IF.text : "",
			pass = mem_pass ? password_IF.text : ""
		}, Formatting.Indented);
		string mac_adress = GetMac();
		var key = Hash(Encoding.UTF8.GetBytes(mac_adress));
		var IV = KDF(Encoding.UTF8.GetBytes(GetMac()), Encoding.UTF8.GetBytes(GetMac()), 16);
		string encrypt = Convert.ToBase64String(EncryptAES(json, key, IV));
		File.WriteAllText(last_auth_info_file, encrypt);
	}

	/// <summary>
	/// set user hashed password for current session
	/// </summary>
	public void SetPassKdf() {
		byte[] hash = Hash(Encoding.UTF8.GetBytes(password_IF.text));
		User.pass_IV = KDF(hash, hash, 32);
		User.pass_kdf = KDF(User.pass_IV, hash, 64);
	}

	/// <summary>
	/// get the mac adress
	/// </summary>
	/// <returns>string mac adress</returns>
	public string GetMac() {
		string macAddr =
		(
				from nic in NetworkInterface.GetAllNetworkInterfaces()
				where nic.OperationalStatus == OperationalStatus.Up
				select nic.GetPhysicalAddress().ToString()
		).FirstOrDefault();
		if (macAddr == null || macAddr == "") {
			macAddr = "NO MAC ADRESS";
		}
		return macAddr;
	}

	/// <summary>
	/// Auth user on the server
	/// </summary>
	public void Auth() {
		if (nickname_IF.text.Length < min_len_nick || password_IF.text.Length < min_len_pass) return;
		logInBT.GetComponent<Button>().interactable = false;
		User.nickname = nickname_IF.text;
		User.InitPrivateKey(); //generate or read RSA keypair for that user
		uWebSocketManager.EmitEv("auth", new { nickname = nickname_IF.text, password = Convert.ToBase64String(Hash(Encoding.UTF8.GetBytes(password_IF.text))), public_rsa = Simplfy_XML_RSA(User.GetDefaultPublicKey()) });
	}

	/// <summary>
	/// add user to contact scroll
	/// </summary>
	/// <param name="name"></param>
	/// <param name="id"></param>
	public void AddContactToList(string name, string id) {
		if (GameObject.Find("contact_" + id) != null) return;
		GameObject go = Instantiate(contact_prefab, contact_scroll.transform);
		go.name = "contact_" + id;
		go.transform.Find("nick").GetComponent<TMP_Text>().text = name;
		go.GetComponent<ContactPrefab>().id = id;
	}

	/// <summary>
	/// remove all users from contact scroll
	/// </summary>
	public void ClearContats() {
		foreach (Transform item in contact_scroll.transform) {
			Destroy(item.gameObject);
		}
		GetComponent<MessageManager>().Focus("");
	}

	/// <summary>
	/// check if you can press "add contact"
	/// </summary>
	public void MakeAddContactEnbled() {
		addContactBT.interactable = new_contact.text.Length > 3;
	}

	/// <summary>
	/// Check if login button is enabled
	/// </summary>
	public void MakeLogInVisible() {
		SetPassKdf();
		logInBT.GetComponent<Button>().interactable = true;

		logInBT.SetActive(password_IF.text.Length >= min_len_pass && nickname_IF.text.Length >= min_len_nick);
		if (!logInBT.activeSelf && password_IF.text.Length > 0 && nickname_IF.text.Length > 0) {
			logInInfo.SetActive(true);
			logInInfo.transform.Find("Txt").GetComponent<TMP_Text>().text = Languages.Get("min_req_log").Replace("#X", min_len_nick.ToString()).Replace("#Y", min_len_pass.ToString());
		} else {
			logInInfo.SetActive(false);
		}
	}

	/// <summary>
	/// request user info to initiate a conversation
	/// </summary>
	public void AddContact() {
		uWebSocketManager.EmitEv("request:user:info", new { nickname = new_contact.text });
		new_contact.text = "";
	}

	/// <summary>
	/// clear error console after 5 seconds
	/// </summary>
	/// <returns></returns>
	IEnumerator ClearConsole() {
		yield return new WaitForSeconds(5);
		while (console.color.a > 0) {
			console.color = new Color(console.color.r, console.color.g, console.color.b, console.color.a - 0.1f);
			Color c = console.transform.parent.GetComponent<Image>().color;
			console.transform.parent.GetComponent<Image>().color = new Color(c.r, c.g, c.b, console.color.a - 0.1f);
			yield return new WaitForSeconds(0.05f);
		}
		console.text = "";
	}

	/// <summary>
	/// display info on console
	/// </summary>
	public void Prompt(string log) {
		StopCoroutine(nameof(ClearConsole));
		console.transform.parent.GetComponent<Image>().enabled = true;
		console.text = log;
		Color c = console.transform.parent.GetComponent<Image>().color;
		console.transform.parent.GetComponent<Image>().color = new Color(c.r, c.g, c.b, 1);
		console.color = new Color(1, 0, 0, 1);
		StartCoroutine(nameof(ClearConsole));
	}

	/// <summary>
	/// set or disable if nick is stored locally for next auth
	/// </summary>
	public void ChangeMemNick() {
		mem_nick = !mem_nick;
		mem_nick_txt.color = mem_nick ? new Color(0.56f, 0.88f, 1) : new Color(0.35f, 0.35f, 0.35f);
		mem_nick_txt.transform.parent.Find("Image").gameObject.SetActive(mem_nick);
	}

	/// <summary>
	/// set or disable if password is stored locally for next auth
	/// /!\ NOT RECOMMENDED /!\
	/// </summary>
	public void ChangeMemPass() {
		mem_pass = !mem_pass;
		mem_pass_txt.color = mem_pass ? new Color(1f, 0.46f, 0.35f) : new Color(0.35f, 0.35f, 0.35f);
		mem_pass_txt.transform.parent.Find("Image").gameObject.SetActive(mem_pass);
	}

	class AutoLog {
		public string nick;
		public string pass;
	}
}
