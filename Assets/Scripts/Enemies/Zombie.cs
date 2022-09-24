using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class Zombie : MonoBehaviour {
	NavMeshAgent agent;
	DateTime start = DateTime.UtcNow;
	internal string id = Guid.NewGuid().ToString();
	int life = 100;
	int damages = 1;
	float hitRate = 0.5f;
	GameObject player;
	Image blood;
	private void Awake() {
		blood = GameObject.Find("BloodTexture").GetComponent<Image>();
		player = GameObject.FindGameObjectWithTag("Player");
		name = id;
		agent = GetComponent<NavMeshAgent>();
		life = (int) Math.Pow((DateTime.UtcNow - start).TotalSeconds, 0.5f) + 100;
		damages = 5 + (int)Math.Pow((DateTime.UtcNow - start).TotalSeconds, 0.4f);
		InvokeRepeating(nameof(DoDamages), UnityEngine.Random.Range(0f, 2f), 1/hitRate);
		if (!GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>().host) return;
		InvokeRepeating(nameof(SetPos), 1, 1);
	}

	/// <summary>
	/// définit la cible du zombie, uniquement si il est instancié par le joueur HOST
	/// </summary>
	void SetPos() {
		if (life <= 0) return;
		List<GameObject> others = GameObject.FindGameObjectsWithTag("OtherPlayer").ToList();
		others.Add(player);

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

	public void DoDamages() {
		if (Vector3.Distance(player.transform.position, this.transform.position) < 3.5f) {
			player.GetComponent<PlayerControl>().TakeDamages(damages);
			StartCoroutine(nameof(DamagesAnim));
			FindObjectOfType<AudioManager>().Play("zombie-hit-" + UnityEngine.Random.Range(0, 2));
		}
	}

	IEnumerator DamagesAnim() {
		blood.color = new Color(1, 1, 1, 1);
		while(blood.color.a > 0) {
			blood.color = new Color(1, 1, 1, blood.color.a - 0.01f);
			yield return new WaitForSeconds(0.01f);
		}
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
