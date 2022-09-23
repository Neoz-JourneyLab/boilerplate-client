using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocket = WebSocketSharp.WebSocket;

// ReSharper disable once InconsistentNaming
public class uWebSocketManager : MonoBehaviour {
	delegate void EventDelegation(string e);
	Dictionary<string, EventDelegation> events = new() {
		{ "pong", WsEvents.Pong },
		{ "plot", WsEvents.Plot },
		{ "flashlight:receive", WsEvents.ChangeFlashlightState },
		{ "zombie", WsEvents.Zombie },
		{ "zombie:dead", WsEvents.ZombieDead },
		{ "player:shot", WsEvents.PlayerShot },
		{ "hit:zombie", WsEvents.HitZombie },
		{ "new:game:available", WsEvents.NewGameAvailable },
		{ "cancel:game", WsEvents.CancelGame},
		{ "joined:game", WsEvents.JoinedGame},
		{ "change:state", WsEvents.ChangeState},
		{ "seeds:items", WsEvents.SeedsItems},
	};
	public static string socketId;
	public WebSocket ws;
	public string uri;
	static uWebSocketManager uws;

	private void Awake() {
		DontDestroyOnLoad(this.gameObject);
	}

	private void Start() {
		InvokeRepeating(nameof(Ping), 1, 1f);
		InitSocket("ws://" + uri + "/");
		uws = GameObject.Find("AppManager").GetComponent<uWebSocketManager>();
	}

	/// <summary>
	/// Ajoute tous les listeners pour les events émis par le serveur
	/// </summary>
	public void InitSocket(string uri) {
		print("Connect on " + uri + "...");
		ws = new WebSocket(uri);
		ws.OnOpen += (sender, e) => {
		};
		ws.OnMessage += (sender, e) => {
			//Le message contient l'évenement (listé dans WsEvents.cs) et les datas en JSON
			Payload payload = JsonUtility.FromJson<Payload>(e.Data.ToString());
			if (payload.id != null && payload.id != socketId) {
				socketId = payload.id;
				Emit("seeds", null);
			}
			if (events.ContainsKey(payload.ev)) {
				//routage de l'event serveur
				UnityMainThread.wkr.AddJob(() => { events[payload.ev](payload.data); });
			}
		};
		ws.OnClose += (sender, e) => {
			Debug.Log("SOCKET CLOSED");
		};
		ws.ConnectAsync();
	}

	int tries = 1;
	DateTime nextTry = DateTime.UtcNow;
	void Ping() {
		if (ws == null) return;
		if (WsEvents.pings.Count > 5) {
			if (nextTry > DateTime.UtcNow) {
				return;
			}
			tries++;
			nextTry = DateTime.UtcNow.AddSeconds(Math.Pow(2, tries));
			//Debug.Log("Next try in " + Math.Pow(2, tries) + "s");
			socketId = "";
			ws.ConnectAsync();
			WsEvents.pings.Clear();
			return;
		}
		tries = 1;
		nextTry = DateTime.UtcNow;
		string ping_id = Guid.NewGuid().ToString();
		WsEvents.pings.Add(ping_id, DateTime.UtcNow);
		Emit("ping", new { ping_id });
	}

	/// <summary>
	/// Emit sur le socket uws
	/// </summary>
	/// <param name="ev">Le nom de l'évenement pour le routage</param>
	/// <param name="data">un objet a passer au serveur</param>
	public void Emit(string ev, object data) {
		/*if (!ws.IsAlive) {
		Debug.Log("socket is not alive !");
		return;
	}*/
		string json = JsonConvert.SerializeObject(new Payload() { ev = ev, id = socketId, data = JsonConvert.SerializeObject(data) });
		ws.Send(json);
	}

	public static void EmitEv(string ev, object data = null) {
		uws.Emit(ev, data);
	}

	public void Close(string reason) {
		Emit("bye", new { reason });
	}
}

/// <summary>
/// Contient le payload pour le serveur, avec l'ID websocket, le nom d'évent et l'objet data "json"
/// </summary>
public class Payload {
	public string data;
	public string ev;
	public string id;
}
