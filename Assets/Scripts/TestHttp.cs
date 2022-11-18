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

public class TestHttp : MonoBehaviour {
  public void Post() {
    StartCoroutine(TestPost("/test", CreateDic(new List<string>() { "test", "test" })));
  }

  static IEnumerator TestPost(string route, IReadOnlyDictionary<string, string> data) {
    UnityWebRequest uwr = GetUwr(route, data);
    yield return uwr.SendWebRequest();

    if (uwr.responseCode != 200) {
      console.Prompt($"Error during {route} : " + uwr.error, Color.red);
      yield break;
    }

    console.Prompt("Réponse du serveur http : " + uwr.downloadHandler.text);
    Response res = JsonConvert.DeserializeObject<Response>(uwr.downloadHandler.text);
    console.Prompt("Variable test : " + res.test);
  }
}

[Serializable]
class Response {
  public string test;
}
