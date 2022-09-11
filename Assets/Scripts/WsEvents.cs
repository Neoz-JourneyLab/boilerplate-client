using System;
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

   public static void Err(string json) {
      string code = JObject.Parse(json)["code"].ToString();
      string message = JObject.Parse(json)["message"].ToString();
      Debug.Log("Error : " + code + " > " + message);
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