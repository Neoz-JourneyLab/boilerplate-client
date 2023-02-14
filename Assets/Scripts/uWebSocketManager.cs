using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using WebSocket = WebSocketSharp.WebSocket;

/// <summary>
/// Manage the web socket event for in and out
/// </summary>
public class uWebSocketManager : MonoBehaviour {
	[SerializeField] GameObject mainBlocker;
	[SerializeField] GameObject reconnectBT;
	[SerializeField] TMP_InputField realmTxt;
	[SerializeField] string socketId;
	public string URI = "";
	public WebSocket ws;
	public bool first = true;
	delegate void EventDelegation(string e);

	/// <summary>
	/// Add all listeners for events emitted by the server
	/// </summary>
	readonly Dictionary<string, EventDelegation> events = new Dictionary<string, EventDelegation>() {
		{ "pong", WsEvents.Pong },
		{ "new:message", WsEvents.NewMessage },
		{ "auth:response", WsEvents.AuthOK },
		{ "auth:error", WsEvents.AuthError },
		{ "user:info", WsEvents.UserInfos },
		{ "message:distributed", WsEvents.MessageDistributed },
		{ "no:more:messages", WsEvents.NoMoreMessage },
		{ "err", WsEvents.Err },
	};

	/// <summary>
	/// initialize a web socket connexion with the realm text file
	/// </summary>
	private void Start() {
		string realm_path = Application.streamingAssetsPath + "/realm.txt";
		if (!File.Exists(realm_path)) {
			File.WriteAllText(realm_path, "ws://localhost:9997");
		}
		URI = File.ReadAllText(realm_path);
		InitSocket(URI);
	}

	/// <summary>
	/// close web socket connection
	/// </summary>
	public void Close() {
		ws.Close();
	}

	//log out, and reset all credentials informations
	public void Logout(bool reco) {
		ws?.CloseAsync();
		first = true;
		GameObject.Find("Canvas").GetComponent<MainClass>().authGroup.SetActive(true);
		GameObject.Find("Nick IF").GetComponent<TMP_InputField>().text = "";
		GameObject.Find("Pass IF").GetComponent<TMP_InputField>().text = "";

		if (reco && File.Exists(Application.streamingAssetsPath + "/last_auths_infos.txt")) {
			File.Delete(Application.streamingAssetsPath + "/last_auths_infos.txt");
		}


		User.nickname = "";
		User.id = "";
		User.users_infos.Clear();
		User.conversations.Clear();
		User.root_memory.Clear();
		User.pass_IV = new byte[0];
		User.pass_kdf = new byte[0];
		GameObject.Find("Canvas").GetComponent<MainClass>().ClearContats();
		if (reco) {
			URI = realmTxt.text;
			InitSocket(URI);
			realmTxt.transform.Find("Text Area").transform.Find("Text").GetComponent<TMP_Text>().color = ColorPalette.Get(Palette.paleOrange);
		} else {
			CancelInvoke(nameof(Ping));
			mainBlocker.SetActive(false);
			reconnectBT.SetActive(true);
			realmTxt.transform.Find("Text Area").transform.Find("Text").GetComponent<TMP_Text>().color = ColorPalette.Get(Palette.lightGray);
		}
	}

	/// <summary>
	/// Create a web socket connection and start ping method
	/// </summary>
	public void InitSocket(string uri) {
		ws = new WebSocket(uri);

		ws.OnOpen += (sender, e) => {
			UnityMainThread.wkr.AddJob(() => {
				GameObject.Find("Canvas").GetComponent<MainClass>().SetInputsLogIng(true);
				InvokeRepeating(nameof(Ping), 0.1f, 1);
				File.WriteAllText(Application.streamingAssetsPath + "/realm.txt", URI);
				realmTxt.transform.Find("Text Area").transform.Find("Text").GetComponent<TMP_Text>().color = ColorPalette.Get(Palette.lime);
				mainBlocker.SetActive(false);
				reconnectBT.SetActive(false);
			});
		};
		ws.OnMessage += (sender, e) => {
			//The message contains the event (listed in WsEvents.cs) and the data in JSON
			Payload payload = JsonUtility.FromJson<Payload>(e.Data.ToString());
			if (payload.id != null && payload.id != socketId) {
				socketId = payload.id;
				if (first) {
					UnityMainThread.wkr.AddJob(() => {
						GameObject.Find("Canvas").GetComponent<MainClass>().Auth();
					});
				} else {
					UnityMainThread.wkr.AddJob(() => {
						GameObject.Find("Canvas").GetComponent<MainClass>().Auth();
					});
				}
			}
			if (!events.ContainsKey(payload.ev)) return;
			//server event routing
			UnityMainThread.wkr.AddJob(() => { events[payload.ev](payload.data); });
		};
		ws.OnClose += (sender, e) => {
			UnityMainThread.wkr.AddJob(() => {
				reconnectBT.SetActive(true);
				realmTxt.transform.Find("Text Area").transform.Find("Text").GetComponent<TMP_Text>().color = ColorPalette.Get(Palette.red);
				WsEvents.GetServerStatusTxt().text = Languages.Get("Reconnecting...");
				GameObject.Find("Canvas").GetComponent<MainClass>().SetInputsLogIng(false);
				WsEvents.GetServerStatusTxt().color = new Color(1, 0.5f, 0);
			});
		};

		ws.ConnectAsync();
	}

	int tries = 1;
	DateTime nextTry = DateTime.UtcNow;
	/// <summary>
	/// Ping the websocket server : if fail/not received, try a reconnection
	/// </summary>
	void Ping() {
		if (ws == null) return;
		if (!ws.IsConnected || !ws.IsAlive || WsEvents.pings.Count > 5) {
			if (nextTry > DateTime.UtcNow) {
				return;
			}
			tries++;
			nextTry = DateTime.UtcNow.AddSeconds(Math.Pow(2, tries));
			socketId = "";
			ws.CloseAsync();
			ws.ConnectAsync();
			WsEvents.pings.Clear();
			realmTxt.transform.Find("Text Area").transform.Find("Text").GetComponent<TMP_Text>().color = ColorPalette.Get(Palette.red);
			mainBlocker.SetActive(true);
			return;
		}
		realmTxt.transform.Find("Text Area").transform.Find("Text").GetComponent<TMP_Text>().color = ColorPalette.Get(Palette.lime);
		mainBlocker.SetActive(false);
		tries = 1;
		nextTry = DateTime.UtcNow;
		string ping_id = Guid.NewGuid().ToString();
		WsEvents.pings.Add(ping_id, DateTime.UtcNow);
		Emit("ping", new { ping_id });
	}

	/// <summary>
	/// Emit on the uws socket
	/// </summary>
	/// <param name="ev">The name of the event for the routing</param>
	/// <param name="data">an object to pass to the server</param>
	public void Emit(string ev, object data) {
		if (!ws.IsAlive) {
			Debug.Log("socket is not alive !");
			return;
		}
		string json = JsonConvert.SerializeObject(new Payload() { ev = ev, id = socketId, data = JsonConvert.SerializeObject(data) });
		ws.SendAsync(json, null);
	}

	/// <summary>
	/// static access this emit function
	/// </summary>
	public static void EmitEv(string ev, object data = null) {
		GameObject.FindGameObjectWithTag("AppManager").GetComponent<uWebSocketManager>().Emit(ev, data);
	}

	public void Close(string reason) {
		Emit("bye", new { reason });
	}
}

/// <summary>
/// Contains the payload for the server, with websocket id, event name and "json" data object
/// </summary>
public class Payload {
	public string data;
	public string ev;
	public string id;
}
