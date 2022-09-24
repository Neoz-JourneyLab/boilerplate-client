using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
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
   public static bool host = true;
   static Dictionary<string, Light> otherPlayersLights = new Dictionary<string, Light>();

   #endregion

   #region listeners
   class UserItem {
      public string id;
      public int quantity;
   }
   public static void PlayerItems(string json) {
      List<UserItem> items = JsonConvert.DeserializeObject<List<UserItem>>(json);
      var inventory = GameObject.FindGameObjectWithTag("Player").GetComponent<InventoryController>();
      inventory.playerItems.Clear();
      foreach (var item in items) {
         InventoryItem itemToAdd = new InventoryItem {
            quantity = item.quantity,
            itemData = ItemCollection.GetItems().First(i => i.id == item.id)
         };
         inventory.playerItems.Add(itemToAdd);
      }
   }

   public static void SeedsItems(string json) {
      List<ItemData> items = JsonConvert.DeserializeObject<List<ItemData>>(json);
      foreach (var item in items) {
         item.SetIcon();
         ItemCollection.Set(item);
      }
   }
   public static void ChangeState(string json) {
      string name = JObject.Parse(json)["name"].ToString();
      GameObject.Find(name).GetComponent<Levier>().ChangeState();
   }
   public static void NewGameAvailable(string json) {
      string nickname = JObject.Parse(json)["nickname"].ToString();
      string id = JObject.Parse(json)["id"].ToString();
      string name = JObject.Parse(json)["name"].ToString();
      string level = JObject.Parse(json)["level"].ToString();
      GameObject.Find("Canvas").GetComponent<Lobby>().NewGame(name, nickname, id, level);
   }

   public static void JoinedGame(string json) {
      print(json);
      string id = JObject.Parse(json)["id"].ToString();
      host = JObject.Parse(json)["host"].ToString().ToLower() == "true";
      GameObject.Find("Canvas").GetComponent<Lobby>().LoadGame(id);
   }

   public static void CancelGame(string json) {
      string id = JObject.Parse(json)["id"].ToString();
      GameObject.Find("Canvas").GetComponent<Lobby>().CancelGame(id);
   }

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

   public static void PlayerShot(string json) {
      string id = JObject.Parse(json)["id"].ToString();
      if (id == uWebSocketManager.socketId) {
         return;
         //player.transform.Find("")
      } else {
         GameObject.Find(id).transform.Find("GunControl").transform.Find("Pistol").GetComponent<Gun>().ShotAnim();
      }
   }

   public static PlayerControl GetPlayer() {
      if (player == null) player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>();
      return player;
   }

   public static void Plot(string json) {
      PlayerPos pos = JsonConvert.DeserializeObject<PlayerPos>(json);
      if (pos.id == uWebSocketManager.socketId) return;
      if (!otherPlayersLights.ContainsKey(pos.id)) {
         otherPlayersLights.Add(pos.id,
            GameObject.Find(pos.id).transform.Find("GunControl").transform.Find("Pistol").transform.Find("Flashlight").GetComponent<Light>()
         );
      }
      otherPlayersLights[pos.id].intensity = pos.flashLight;
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
      if (!GetPlayer().host) return;
      try {
         var js = JObject.Parse(json);
         string zid = js["zid"].ToString();
         int damages = (int)js["damages"];
         GameObject.Find(zid).GetComponent<Zombie>().TakeDamages(damages);
      } catch (Exception) { }
   }
   public static void Zombie(string json) {
      var js = JObject.Parse(json);
      string zid = js["zid"].ToString();
      float x = (float)js["x"];
      float z = (float)js["z"];

      float speed = (float)js["speed"];
      if (GameObject.Find(zid) == null) {
         float sx = (float)js["spawnX"];
         float sz = (float)js["spawnZ"];
         if (zombiePrefab == null) zombiePrefab = GameObject.Find("ZombieSpawner").GetComponent<ZombieSpawner>().zombiePrefab;
         GameObject zombie = Instantiate(zombiePrefab, new Vector3(sx, 0, sz), new Quaternion());
         zombie.GetComponent<Zombie>().id = zid;
         zombie.name = zid;
      }
      GameObject.Find(zid).GetComponent<Zombie>().SetFromServer(x, z, speed);
   }

   public static void ChangeFlashlightState(string json) {
      string uuid = JObject.Parse(json)["uuid"].ToString();
      bool flashlightState = (bool)JObject.Parse(json)["state"];
      GameObject.Find(uuid).transform.Find("GunControl").transform.Find("Pistol").transform.Find("Flashlight").gameObject.SetActive(flashlightState);
   }
   #endregion
}

#region class
public class PlayerPos {
   public float x;
   public float ry;
   public float z;
   public float flashLight;

   public float nx;
   public float nry;
   public float nz;

   public string id;
}
#endregion