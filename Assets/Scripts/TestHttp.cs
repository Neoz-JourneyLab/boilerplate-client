using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using static Tools;

/// <summary>
/// NOT USED BECAUSE NGROK IS NOT FREE FOR MULTIPLE PROTOCOLS
/// BUT THAT COULD BE COOL TO CHECK SERVER STATUS WITH THAT INSTEAD OF WS
/// </summary>
public class TestHttp : MonoBehaviour {
	public void Post() {
		StartCoroutine(TestPost("/server:status", new Dictionary<string, string>()));
	}

	private void Start() {
	}

	static IEnumerator TestPost(string route, Dictionary<string, string> data) {
		UnityWebRequest uwr = GetUwr(route, data);
		yield return uwr.SendWebRequest();

		if (uwr.responseCode != 200) {
			yield break;
		}

		Response res = JsonConvert.DeserializeObject<Response>(uwr.downloadHandler.text);

		if (res.status == "online") {
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
