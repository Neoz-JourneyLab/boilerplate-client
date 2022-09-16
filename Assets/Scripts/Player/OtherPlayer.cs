using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherPlayer : MonoBehaviour {
	public readonly PlayerPos pos = new PlayerPos();
	DateTime lastRefresh = DateTime.UtcNow;
	public float deltaMs = 100;
	private void Update() {
		//if (pos.x == pos.nx && pos.z == pos.nz) return;
		try {
			//si on a pas refresh depuis 50ms, on est à 50% sur les 100ms attendues
			float timeSinceLastRefresh = (float)(DateTime.UtcNow - lastRefresh).TotalMilliseconds;
			float percentUntilNextRefresh = timeSinceLastRefresh / deltaMs; //50 / 100 = 0.5
			float x = Mathf.Lerp(pos.x, pos.nx, percentUntilNextRefresh);
			float z = Mathf.Lerp(pos.z, pos.nz, percentUntilNextRefresh);
			float y = Mathf.Lerp(pos.ry, pos.nry, percentUntilNextRefresh);
			transform.position = new Vector3(x, transform.position.y, z);
			transform.localEulerAngles = new Vector3(0, y, 0);
		} catch(Exception) {
			Debug.LogError("Err : " + pos.ry + " ; " + pos.nry);
		}
	}

	public void Refresh(PlayerPos newPos) {
		if (newPos.x == null || newPos.z == null || newPos.ry == null) return;

		pos.x = pos.nx;
		pos.z = pos.nz;
		pos.ry = pos.nry;

		pos.nx = newPos.x;
		pos.nz = newPos.z;
		pos.nry = newPos.ry;
		//ex: 100 ms entre les refresh, donc on stocke dans deltaMs le temps attendu entre les refresh
		deltaMs = (float)(DateTime.UtcNow - lastRefresh).TotalMilliseconds;
		lastRefresh = DateTime.UtcNow;
	}
}
