using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// usefull tools for all purposes
/// </summary>
public static class Tools {
	/// <summary>
	/// parse a date from a specific "dd/MM/yyyy HH:mm:ss" format
	/// </summary>
	public static DateTime GetDateFromStr(string dateJson) {
		return DateTime.ParseExact(dateJson, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Get Unity Web Request for Http POST [UNUSED]
	/// </summary>
	internal static UnityWebRequest GetUwr(string route, IReadOnlyDictionary<string, string> data) {
		string json = JsonConvert.SerializeObject(data);
		string uri = "localhost:9998" + route;
		UnityWebRequest uwr = new UnityWebRequest(uri) { method = "POST", uploadHandler = new UploadHandlerRaw(new UTF8Encoding().GetBytes(json)), downloadHandler = new DownloadHandlerBuffer() };
		uwr.SetRequestHeader("Content-Type", "application/json");
		return uwr;
	}
}
