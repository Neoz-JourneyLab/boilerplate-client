using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour {
	public GameObject zombiePrefab;
	[SerializeField] float spawnPerMinute = 10; //10 par minute
	[SerializeField] int max = 10; //10 par minute
	private void Start() {
		if (!GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>().host) return;
		InvokeRepeating(nameof(Spawn), 0, 1f);
	}

	void Spawn() {
		float spawnLuck = spawnPerMinute / 6; //10 / 6 = 1.6
		float rand = Random.Range(0, 10f);
		if (rand > spawnLuck) {
			return;
		}
		if (transform.childCount >= max) return;
		GameObject go = Instantiate(zombiePrefab, transform.position, new Quaternion());
		go.transform.parent = transform;
	}
}
