using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Zombie : MonoBehaviour {
	NavMeshAgent agent;
	DateTime start = DateTime.UtcNow;
	public string id = Guid.NewGuid().ToString();

	private void Awake() {
		agent = GetComponent<NavMeshAgent>();
		if (GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>().host) {
			InvokeRepeating(nameof(SetPos), 1, 1);
		}
	}

	void SetPos() {
		agent.destination = GameObject.FindGameObjectWithTag("Player").transform.position;
		agent.speed = (Mathf.Pow((int)(DateTime.UtcNow - start).TotalSeconds, 0.3f) + 2) / 2;
		uWebSocketManager.EmitEv("send:zombie:target", new {
			agent.destination.x,
			agent.destination.z,
			agent.speed,
			zid = id
		});
	}

	public void SetFromServer(float x, float z, float speed) {
		agent.destination = new Vector3(x, agent.destination.y, z);
		agent.speed = speed;
	}
}
