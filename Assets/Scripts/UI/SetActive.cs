using System.Collections.Generic;
using UnityEngine;

public class SetActive : MonoBehaviour {
	[SerializeField] List<GameObject> gos;
	public void Set(bool state) {
		foreach (var go in gos) {
			go.SetActive(state);
		}
	}

	public void Invert() {
		foreach (var go in gos) {
			go.SetActive(!go.activeSelf);
		}
	}
}
