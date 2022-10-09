using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Highlight : MonoBehaviour {

	public bool canMove = true;
	public bool canAttack = false;

	public void Click() {
		if (canMove) {
			int deltaX = int.Parse(name.Split(';')[0]);
			int deltaY = int.Parse(name.Split(';')[1].Split('_')[0]);
			transform.parent.GetComponent<Unit>().Move(deltaX, deltaY);

			if (SceneManager.GetActiveScene().name == "Donner des ordres") {
				FindObjectOfType<DonnerDesOrdres>().FirstMove();
			}
		}
	}
}
