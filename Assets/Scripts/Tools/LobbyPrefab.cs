using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyPrefab : MonoBehaviour {
	public void Click() {
		GameObject.Find("Canvas").GetComponent<Lobby>().CancelOrJoin(name);
	}
}
