using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValidMotion : MonoBehaviour {
	public Unit target;

	private void Awake() {
		transform.Find("go").gameObject.SetActive(false);
	}
	public void Validate() {
		target.ValidMove();
	}
	public void Cancel() {
		target.CancelMove();
	}
}
