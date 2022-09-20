using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : MonoBehaviour {
	[SerializeField] GameObject wall;
	bool closed = true;
	public int playerOnPlate = 0;
	[SerializeField] bool definitive = false;

	private void OnTriggerEnter(Collider other) {
		if (other.tag != "Player" && other.tag != "OtherPlayer") return;
		playerOnPlate++;
		if (!closed) return;
		closed = false;
		if (playerOnPlate > 1) return; //déjà quelq'un
		StopCoroutine(nameof(CloseDor));
		StartCoroutine(nameof(OpenDor));
	}

	private void OnTriggerExit(Collider other) {
		if(definitive) return;
		if (other.tag != "Player" && other.tag != "OtherPlayer") return;
		playerOnPlate--;
		if (closed) return;
		closed = true;
		if (playerOnPlate > 0) return; //toujours quelq'un
		StartCoroutine(nameof(CloseDor));
		StopCoroutine(nameof(OpenDor));
	}

	IEnumerator CloseDor() {
		wall.GetComponent<AudioSource>().Play();
		while (wall.transform.position.y < 4f) {
			wall.transform.position = new Vector3(wall.transform.position.x, wall.transform.position.y + 0.1f, wall.transform.position.z);
			yield return new WaitForSeconds(0.01f);
		}
	}
	IEnumerator OpenDor() {
		wall.GetComponent<AudioSource>().Play();
		while (wall.transform.position.y > -7.5f) {
			wall.transform.position = new Vector3(wall.transform.position.x, wall.transform.position.y - 0.1f, wall.transform.position.z);
			yield return new WaitForSeconds(0.01f);
		}
	}
}
