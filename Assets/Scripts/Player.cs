using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
	int speed = 5;
	// Update is called once per frame
	void Update() {
		if (Input.GetKey(KeyCode.D)) {
			GetComponent<Rigidbody>().MovePosition(transform.position + new Vector3(1 * Time.deltaTime * speed, 0, 0));
		}
		if (Input.GetKey(KeyCode.Q)) {
			GetComponent<Rigidbody>().MovePosition(transform.position + new Vector3(-1 * Time.deltaTime * speed, 0, 0));
		}
		if (Input.GetKey(KeyCode.Z)) {
			GetComponent<Rigidbody>().MovePosition(transform.position + new Vector3(0, 0, 1 * Time.deltaTime * speed));
		}
		if (Input.GetKey(KeyCode.S)) {
			GetComponent<Rigidbody>().MovePosition(transform.position + new Vector3(0, 0, -1 * Time.deltaTime * speed));
		}
	}
}
