using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControl : MonoBehaviour {
	// Start is called before the first frame update
	public float speed;
	[Range(0, 1)]
	public float cameraOffset;
	public float cameraHeight;
	public float footstepDelay;
	public bool host = true;
	int life = 100;

	public AudioSource footstepAudio;

	Vector2 moveDirection = Vector2.zero;
	Vector2 mousePosition = Vector2.zero;
	Vector3 movement;

	Transform camera;
	Transform gunHolder;

	Rigidbody rb;

	float nextStep;
	void Start() {
		rb = GetComponent<Rigidbody>();
		camera = Camera.main.transform;
		gunHolder = transform.Find("GunHolder");
		host = WsEvents.host;
		if (host) {
			gameObject.transform.position = GameObject.Find("PLAYER_1").transform.position;
			foreach (var item in GameObject.FindGameObjectsWithTag("spawner")) {
				item.GetComponent<ZombieSpawner>().StartSpawn();
			}
		} else {
			gameObject.transform.position = GameObject.Find("PLAYER_2").transform.position;
			foreach (var item in GameObject.FindGameObjectsWithTag("spawner")) {
				//Destroy(item);
			}
		}
	}

	public void Update() {
		movement = new Vector3(moveDirection.x, 0, moveDirection.y);

		Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cameraHeight));

		camera.position = Vector3.Lerp(new Vector3(transform.position.x, transform.position.y + cameraHeight, transform.position.z), new Vector3(worldPos.x, cameraHeight, worldPos.z), cameraOffset);

		Vector3 pointToLookAt = new Vector3(worldPos.x, transform.position.y, worldPos.z);
		Vector3 pointToLookAtGunHold = new Vector3(worldPos.x, gunHolder.transform.position.y, worldPos.z);
		transform.LookAt(pointToLookAt);

		if (Vector3.Distance(gunHolder.transform.position, pointToLookAtGunHold) < 0.8f)
			gunHolder.transform.localRotation = Quaternion.identity;
		else
			gunHolder.LookAt(pointToLookAtGunHold);

		if (movement == Vector3.zero)
			return;

		if (nextStep < Time.time) {
			nextStep = Time.time + footstepDelay;
			GameObject.FindObjectOfType<AudioManager>().PlaySound(footstepAudio);
		}


	}

	public void TakeDamages(int dmg) {
		life -= dmg;
		if (life < 0) SceneManager.LoadScene("Lobby");
		print("You took " + dmg + " dmg");
	}

	// Update is called once per frame
	void FixedUpdate() {
		rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
	}

	private void OnMove(InputValue value) {
		moveDirection = value.Get() != null ? (Vector2)value.Get() : Vector2.zero;
	}

	private void OnMousePosition(InputValue value) {
		mousePosition = value.Get() != null ? (Vector2)value.Get() : Vector2.zero;
	}


}
