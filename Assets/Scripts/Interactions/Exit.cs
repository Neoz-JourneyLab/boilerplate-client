using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Exit : MonoBehaviour {
	List<string> playersHits = new List<string>();
	private void OnTriggerEnter(Collider other) {
		if (other.tag != "Player" && other.tag != "OtherPlayer") return;
		if (other.tag == "Player" && !playersHits.Contains(uWebSocketManager.socketId)) playersHits.Add(uWebSocketManager.socketId);
		if (other.tag == "OtherPlayer" && playersHits.Contains(other.name)) playersHits.Add(other.name);
		if(playersHits.Count == 2) {
			uWebSocketManager.EmitEv("victory", new { SceneManager.GetActiveScene().name });
			SceneManager.LoadScene("Lobby");
		}
	}
}
