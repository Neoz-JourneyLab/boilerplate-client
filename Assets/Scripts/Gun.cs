using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour {
	public float fireRate;
	public float capacity;
	public float maxCapacity;
	public float reloadTime;
	public int damages;

	public bool automatic;

	public Transform muzzle;

	public ParticleSystem particles;
	public Light muzzleFlash;

	float nextShot;
	// Start is called before the first frame update
	void Start() {
		nextShot = Time.time;
		capacity = maxCapacity;
	}

	// Update is called once per frame
	void Update() {

	}

	public Transform Shoot() {
		if (capacity <= 0)
			return null;
		if (nextShot > Time.time)
			return null;

		nextShot = Time.time + fireRate;
		capacity--;

		ShotAnim();

		Ray ray = new Ray(muzzle.transform.position, muzzle.transform.forward);
		RaycastHit hit;

		Debug.DrawRay(ray.origin, ray.direction, Color.red, 0.5f);

		if (!Physics.Raycast(ray, out hit))
			return null;

		return hit.collider.transform;
	}

	public void ShotAnim() {
		muzzleFlash.gameObject.SetActive(true);
		Invoke(nameof(DisableLight), 0.05f);
		particles.Play();
		uWebSocketManager.EmitEv("shot");
	}

	void DisableLight() {
		muzzleFlash.gameObject.SetActive(false);
	}

	public void Reload() {
		Invoke(nameof(ReloadEnd), reloadTime);
	}

	void ReloadEnd() {
		if (capacity == 0)
			capacity = maxCapacity;
		else
			capacity = maxCapacity + 1;
	}
}
