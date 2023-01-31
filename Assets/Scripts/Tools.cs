using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using WebSocketSharp;

public static class Tools {
	public static DateTime GetDateFromStr(string dateJson) {
		return DateTime.ParseExact(dateJson, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
	}

	internal static UnityWebRequest GetUwr(string route, IReadOnlyDictionary<string, string> data) {
		string json = JsonConvert.SerializeObject(data);
		string uri = "localhost:9998" + route;
		UnityWebRequest uwr = new UnityWebRequest(uri) { method = "POST", uploadHandler = new UploadHandlerRaw(new UTF8Encoding().GetBytes(json)), downloadHandler = new DownloadHandlerBuffer() };
		uwr.SetRequestHeader("Content-Type", "application/json");
		return uwr;
	}

	internal static Dictionary<string, string> CreateDic(List<string> keyVal) {
		Dictionary<string, string> dic = new Dictionary<string, string>();
		dic.Add("passhash", PlayerPrefs.GetString("pass"));
		dic.Add("nickhash", PlayerPrefs.GetString("nick"));

		for (int i = 0; i < keyVal.Count; i += 2) dic.Add(keyVal[i], keyVal[i + 1]);
		return dic;
	}
}
