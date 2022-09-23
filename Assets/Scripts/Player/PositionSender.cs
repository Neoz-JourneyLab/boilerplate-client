using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionSender : MonoBehaviour {
	Transform player;
	[SerializeField] GameObject otherPlayerPrefab;
	public Dictionary<string, GameObject> otherPlayers = new Dictionary<string, GameObject>();
	static PositionSender instance;
	public Light flash = null;

	private void Start() {
		player = transform;
		InvokeRepeating(nameof(SendPlot), 1, 0.2f);
	}

	void SendPlot() {
		uWebSocketManager.EmitEv("send:position", new {
			player.position.x,
			player.position.z,
			ry = player.localEulerAngles.y,
			flashLight = flash == null ? 0 : flash.intensity
		});
	}

	public void PlotOther(PlayerPos pos) {
		GameObject newPlayer;
		if (otherPlayers.ContainsKey(pos.id)) {
			newPlayer = otherPlayers[pos.id];
		} else {
			newPlayer = Instantiate(otherPlayerPrefab);
			newPlayer.transform.localPosition = new Vector3(0, transform.localPosition.y, 0);
			OtherPlayer op = newPlayer.GetComponent<OtherPlayer>();
			op.pos.nry = pos.ry;
			op.pos.nz = pos.z;
			op.pos.nx = pos.x;
			op.pos.id = pos.id;
			newPlayer.name = pos.id;
			otherPlayers.Add(pos.id, newPlayer);
		}
		newPlayer.GetComponent<OtherPlayer>().Refresh(pos);
	}

	public static PositionSender Get() {
		if (instance == null) {
			instance = GameObject.FindGameObjectWithTag("Player").GetComponent<PositionSender>();
		}
		return instance;
	}
}
