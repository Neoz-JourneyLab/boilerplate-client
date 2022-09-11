using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Zombie : MonoBehaviour {
	NavMeshAgent agent;
	DateTime start = DateTime.UtcNow;
	internal string id = Guid.NewGuid().ToString();
	int life = 100;

	private void Awake() {
		name = id;
		agent = GetComponent<NavMeshAgent>();
		if (!GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>().host) return;
		InvokeRepeating(nameof(SetPos), 1, 1);
	}

	/// <summary>
	/// définit la cible du zombie, uniquement si il est instancié par le joueur HOST
	/// </summary>
	void SetPos() {
		if (life <= 0) return;
		List<GameObject> others = GameObject.FindGameObjectsWithTag("OtherPlayer").ToList();
		others.Add(GameObject.FindGameObjectWithTag("Player"));

		Transform target = others.OrderBy(o => Vector3.Distance(o.transform.position, transform.position)).First().transform;

		agent.destination = target.transform.position;
		agent.speed = (Mathf.Pow((int)(DateTime.UtcNow - start).TotalSeconds, 0.3f) + 2) / 2;
		uWebSocketManager.EmitEv("send:zombie:target", new {
			spawnX = transform.parent.position.x,
			spawnZ = transform.parent.position.z,
			agent.destination.x,
			agent.destination.z,
			agent.speed,
			zid = id
		});
	}

	public void TakeDamages(int dmg) {
		life -= dmg;
		if (life <= 0) {
			uWebSocketManager.EmitEv("zombie:dead", new {
				zid = id
			});
		}
	}

	public void SetFromServer(float x, float z, float speed) {
		agent.destination = new Vector3(x, agent.destination.y, z);
		agent.speed = speed;
	}
}
