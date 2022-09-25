using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class casePrefab : MonoBehaviour {
	public int x;
	public int y;
	public string takenBy = "";
	public void Over() {
		GameObject.Find("TXT").GetComponent<TMP_Text>().text = $"{x},{y} (" + takenBy + ")";
	}
}
