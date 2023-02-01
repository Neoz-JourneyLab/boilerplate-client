using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SetWord : MonoBehaviour {
	[SerializeField] bool firstCap = true;
	[SerializeField] string word;

	private void Start() {
		TMP_Text txt = GetComponent<TextMeshProUGUI>() == null ? 
			GetComponentInChildren<TextMeshProUGUI>() : GetComponent<TextMeshProUGUI>();
		if (txt == null) {
			print("/!\\ text for " + gameObject.name + " is null");
			return;
		}
		if (word == "") {
			word = txt.text;
		}
		string w = Languages.Get(word, firstCap);
		txt.text = w;
	}
}
