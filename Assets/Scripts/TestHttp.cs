using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static Crypto;
using static Tools;
using static MainClass;
using System.IO;

public class TestHttp : MonoBehaviour {
  public void Post() {
    StartCoroutine(TestPost("/server:status", new Dictionary<string, string>()));
  }

  private void Start() {
		//Post();


		//GameObject.FindGameObjectWithTag("AppManager").GetComponent<uWebSocketManager>().Initialisation();
	}
  
  static IEnumerator TestPost(string route, Dictionary<string, string> data) {
    UnityWebRequest uwr = GetUwr(route, data);
    yield return uwr.SendWebRequest();

    if (uwr.responseCode != 200) {
      yield break;
    }

    Response res = JsonConvert.DeserializeObject<Response>(uwr.downloadHandler.text);

    if(res.status == "online") {
      //GameObject.FindGameObjectWithTag("AppManager").GetComponent<uWebSocketManager>().Initialisation();
    }
  }
}

[Serializable]
class Response {
  public string status;
  public string client_version;
  public string version;
}
