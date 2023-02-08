using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Languages : MonoBehaviour {
	static List<Word> words = new List<Word>();
	static SystemLanguage language;
	static bool init = false;

	public static void Init(SystemLanguage lang) {
		language = lang;
#if UNITY_EDITOR || PLATFORM_STANDALONE_WIN
		string filePath = Path.Combine(Application.streamingAssetsPath, "language.json");
#elif UNITY_ANDROID
            string filePath = Path.Combine ("jar:file://" + Application.dataPath + "!assets/", "language.json");
#endif
		string dataAsJson = "";
#if UNITY_EDITOR || UNITY_IOS || PLATFORM_STANDALONE_WIN
		if (File.Exists(filePath)) {
			dataAsJson = File.ReadAllText(filePath);
		}
#elif UNITY_ANDROID
            WWW reader = new WWW (filePath);
            while (!reader.isDone) {
            }
            dataAsJson = reader.text;
#endif
		init = true;
		if (dataAsJson == "") return;
		words = JsonConvert.DeserializeObject<List<Word>>(dataAsJson);
		words = words.Where(w => w.fr != "").ToList();
	}

	public static string Get(string alias, bool firstCap = true) {
		if (alias == "") {
			Debug.Log("Empy asking !");
			return "";
		}
		Word word = words.Find(w => w.alias == alias);

		if (word == null) {
			words.Add(new Word() { alias = alias, en = alias, fr = "" });

#if UNITY_EDITOR
			if (init)
				File.WriteAllText("Assets/StreamingAssets/language.json", JsonConvert.SerializeObject(words, Formatting.Indented));
#endif
			return alias;
		}

		string w;
		if (language == SystemLanguage.French && !string.IsNullOrWhiteSpace(word.fr)) w = word.fr;
		else w = !string.IsNullOrWhiteSpace(word.en) ? word.en : alias;

		if (!firstCap) return w;
		try {
			w = w[0].ToString().ToUpper()[0] + w.Substring(1);
		} catch (Exception) {
			Debug.Log("Cannot cap world " + w);
		}

		return w;
	}

	[Serializable]
	public class Word {
		public string alias;
		public string en;
		public string fr;
	}
}
