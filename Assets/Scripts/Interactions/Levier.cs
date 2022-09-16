using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Levier : MonoBehaviour {
	[SerializeField] GameObject wall;
	bool closed = true;
	[SerializeField] TMP_Text interactionText;
	bool inRange = false;

	private void OnTriggerEnter(Collider other) {
		if (other.tag != "Player" && other.tag != "OtherPlayer") return;
		interactionText.text = closed ? "Activer" : "Désactiver";
		inRange = true;
	}

	void OnAction() {
		if (!inRange) return;
		closed = !closed;
		if (!closed) {
			StopCoroutine(nameof(CloseDor));
			StartCoroutine(nameof(OpenDor));
		} else {
			StartCoroutine(nameof(CloseDor));
			StopCoroutine(nameof(OpenDor));
		}
		interactionText.text = closed ? "Activer" : "Désactiver";
	}

	private void OnTriggerExit(Collider other) {
		if (other.tag != "Player" && other.tag != "OtherPlayer") return;
		interactionText.text = "";
		inRange = false;
	}

	IEnumerator CloseDor() {
		GetComponent<AudioSource>().Play();
		while (wall.transform.position.y < 4f) {
			wall.transform.position = new Vector3(wall.transform.position.x, wall.transform.position.y + 0.1f, wall.transform.position.z);
			yield return new WaitForSeconds(0.01f);
		}
	}
	IEnumerator OpenDor() {
		GetComponent<AudioSource>().Play();
		while (wall.transform.position.y > -7.5f) {
			wall.transform.position = new Vector3(wall.transform.position.x, wall.transform.position.y - 0.1f, wall.transform.position.z);
			yield return new WaitForSeconds(0.01f);
		}
	}
}
