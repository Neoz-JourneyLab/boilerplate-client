using UnityEngine;

public class BackgroundImage : MonoBehaviour {
	[SerializeField] float maxpos;
	[SerializeField] float speed;

	private void Update() {
		transform.localPosition = new Vector3(transform.localPosition.x, (Time.time * speed) % maxpos, transform.localPosition.z);
	}
}
