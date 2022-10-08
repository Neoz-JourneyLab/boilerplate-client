using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Dialogs : MonoBehaviour {
	[SerializeField] Image head;
	[SerializeField] GameObject background;
	[SerializeField] TMP_Text text;
	Queue<(string, Sprite)> nexts = new Queue<(string, Sprite)>();
	Image img;
	Action doThen;

	public void Prompt(List<(string, Sprite)> list, Action doAtEnd = null) {
		background.SetActive(true);
		nexts = new Queue<(string, Sprite)>(list);
		doThen = doAtEnd;
		Next();
	}

	private void Update() {
		if (!background.activeInHierarchy) return;
		if (img == null) img = background.transform.Find("DialogBG").transform.Find("Next").GetComponent<Image>();
		img.color = new Color(img.color.r, img.color.g, img.color.b, Mathf.Cos(Time.time * 3));
	}

	public void Next() {
		if (nexts.Count <= 0) {
			Close();
			doThen?.Invoke();
			return;
		}
		var item = nexts.Dequeue();
		if(item.Item2 != null) head.sprite = item.Item2;
		text.text = item.Item1;
	}

	public void Close() {
		text.text = "";
		background.SetActive(false);
	}
}
