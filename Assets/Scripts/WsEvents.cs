﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Tools;

/// <summary>
/// Contient tous les évenements Uws
/// </summary>
public class WsEvents : MonoBehaviour {

	#region Vars

	static int latency;
	public static readonly Dictionary<string, DateTime> pings = new Dictionary<string, DateTime>();
	static TMP_Text serverStatus;
	static GameObject zombiePrefab = null;
	static PlayerControl player = null;

	#endregion

	#region listeners
	 public static void Pong(string json) {
		string pong = JObject.Parse(json)["ping_id"].ToString();
		string serverTime = JObject.Parse(json)["server_time"].ToString();
		if (!pings.ContainsKey(pong)) throw new Exception("ping ID not found: " + pong);
		latency = (int)(DateTime.UtcNow - pings[pong]).TotalMilliseconds / 2;
		pings.Remove(pong);
		if (serverStatus == null) {
			serverStatus = GameObject.Find("server infos").GetComponent<TMP_Text>();
		}
		serverStatus.text = $"server : {GetDateFromStr(serverTime).ToShortDateString() + " " + GetDateFromStr(serverTime).ToLongTimeString()}, {latency}ms";
	}

	public static void Plot(string json) {
		PlayerPos pos = JsonConvert.DeserializeObject<PlayerPos>(json);
		if (pos.id == uWebSocketManager.socketId) return;
		PositionSender.Get().PlotOther(pos);
	}

	public static void ZombieDead(string json) {
		string zid = JObject.Parse(json)["zid"].ToString();
		Destroy(GameObject.Find(zid));
	}

	public static void Err(string json) {
		string code = JObject.Parse(json)["code"].ToString();
		string message = JObject.Parse(json)["message"].ToString();
		Debug.Log("Error : " + code + " > " + message);
	}
	public static void HitZombie(string json) {
		if (player == null) player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>();
		if (!player.host) return;

		var js = JObject.Parse(json);
		string zid = js["zid"].ToString();
		int damages = (int) js["damages"];

		GameObject.Find(zid).GetComponent<Zombie>().TakeDamages(damages);
	}
	public static void Zombie(string json) {
		var js = JObject.Parse(json);
		string zid = js["zid"].ToString();
		float x = (float) js["x"];
		float z = (float) js["z"];
		float speed = (float) js["speed"];
		if (GameObject.Find(zid) == null) {
			if(zombiePrefab == null) zombiePrefab = GameObject.Find("ZombieSpawner").GetComponent<ZombieSpawner>().zombiePrefab;
			GameObject zombie = Instantiate(zombiePrefab, new Vector3(x, 0, z), new Quaternion());
			zombie.name = zid;
		}
		GameObject.Find(zid).GetComponent<Zombie>().SetFromServer(x, z, speed);
	}

	public static void ChangeFlashlightState(string json) {
		string uuid = JObject.Parse(json)["uuid"].ToString();
		bool flashlightState = (bool)JObject.Parse(json)["state"];
		GameObject.Find(uuid).transform.Find("Flashlight").gameObject.SetActive(flashlightState);
	}
	#endregion
}

#region class
public class PlayerPos {
	public float x;
	public float ry;
	public float z;

	public float nx;
	public float nry;
	public float nz;

	public string id;
}
#endregion