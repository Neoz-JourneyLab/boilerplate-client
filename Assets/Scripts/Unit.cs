using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Unit : MonoBehaviour {
	[SerializeField] GameObject highlight;
	[SerializeField] List<Sprite> idle;
	[SerializeField] List<Sprite> selected;
	bool isSelected = false;
	public int pm = 3;
	public bool moved = false;
	TilesManager tm;

	private void Start() {
		tm = FindObjectOfType<TilesManager>();
		Fix();
		if (idle.Count > 0) InvokeRepeating(nameof(Animation), 0, 0.25f);
	}

	int n = 0;
	void Animation() {
		GetComponent<SpriteRenderer>().sprite = isSelected ? selected[n++ % selected.Count] : idle[n++ % idle.Count];
		if (n > 1000000) n = 0;
	}

	public void Fix() {
		float x = Mathf.Floor(transform.position.x) + 0.5f;
		float y = Mathf.Floor(transform.position.y) + 0.5f;
		transform.position = new Vector3(x, y, 0);
	}

	public void OnPointerClick() {
		if (moved || isSelected) return;

		if (SceneManager.GetActiveScene().name == "Donner des ordres") {
			FindObjectOfType<DonnerDesOrdres>().FirstSelect();
		}
		isSelected = true;
		foreach (var item in GameObject.FindGameObjectsWithTag("highlight")) {
			Destroy(item);
		}
		posOk.Clear();
		RecursiveMotion(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y), pm + 1);
		for (int x = -pm; x <= pm; x++) {
			for (int y = -pm; y <= pm; y++) {
				if (Mathf.Abs(x) + Mathf.Abs(y) > pm || (x == 0 && y == 0)) continue;

				GameObject hl = Instantiate(highlight, this.transform);
				hl.name = x + ";" + y + "_hl";
				hl.transform.position = new Vector3(transform.position.x + x, transform.position.y + y, 0);
				int xDest = Mathf.FloorToInt(transform.position.x) + x;
				int yDest = Mathf.FloorToInt(transform.position.y) + y;
				if (posOk.FirstOrDefault(p => p.x == xDest && p.y == yDest) == null) {
					hl.GetComponent<SpriteRenderer>().color = new Color(0.9f, 0.2f, 0, 0.6f);
					hl.GetComponent<Highlight>().canMove = false;
				} else {
					hl.GetComponent<Highlight>().canMove = true;
				}
			}
		}
	}
	class Pos {
		public int x;
		public int y;
		public int pm;
	}
	List<Pos> posOk = new List<Pos>();
	public void RecursiveMotion(int xPos, int yPos, int pm) {
		if (pm <= 0) {
			return;
		}

		var posAlready = posOk.FirstOrDefault(p => p.x == xPos && p.y == yPos);
		//si on a déjà testé cette case
		if (posAlready != null) {
			//si on a plus de PM actuellement :
			if (posAlready.pm < pm) posAlready.pm = pm;
			else {
				//sinon, on a déjà fait des tests en meilleurs conditions
				return;
			}
		} else {
			//jamais testé cette case !
			posOk.Add(new Pos() { x = xPos, y = yPos, pm = pm });
		}

		//si montagnes, pm doit être > 1 (2 pm mini)
		//si rien, pm doit être > 0 (1 pm mini)
		RecursiveMotion(xPos + 1, yPos, pm - (1 + tm.Difficulty(xPos + 1, yPos)));
		RecursiveMotion(xPos - 1, yPos, pm - (1 + tm.Difficulty(xPos - 1, yPos)));
		RecursiveMotion(xPos, yPos - 1, pm - (1 + tm.Difficulty(xPos, yPos - 1)));
		RecursiveMotion(xPos, yPos + 1, pm - (1 + tm.Difficulty(xPos, yPos + 1)));
	}

	public void Move(int x, int y) {
		foreach (var item in GameObject.FindGameObjectsWithTag("highlight")) {
			Destroy(item);
		}
		xDest = (transform.position.x + x);
		yDest = (transform.position.y + y);
		cancelX = x;
		cancelY = y;
		StartCoroutine(nameof(AnimMove));
	}
	float xDest;
	float yDest;
	IEnumerator AnimMove() {
		float i = Time.time;
		float startX = transform.position.x;
		float startY = transform.position.y;
		while (true) {
			float x = Mathf.Lerp(startX, xDest, (Time.time - i)*2);
			float y = Mathf.Lerp(startY, yDest, (Time.time - i)*2);
			transform.position = new Vector3(x, y, 0);
			yield return new WaitForSeconds(0.01f);
			if (Time.time - i >= 0.5f) break;
		}
		transform.position = new Vector3(xDest, yDest, 0);
		GameObject.Find("ValidMotion").transform.Find("go").gameObject.SetActive(true);
		GameObject.Find("ValidMotion").GetComponent<ValidMotion>().target = this;
	}

	int cancelX;
	int cancelY;
	public void CancelMove() {
		transform.position = new Vector3(transform.position.x - cancelX, transform.position.y - cancelY, 0);
		cancelX = 0;
		cancelY = 0;
		EndMove();
	}
	public void ValidMove() {
		if (SceneManager.GetActiveScene().name == "Donner des ordres") {
			FindObjectOfType<DonnerDesOrdres>().FirstValidate();
		}
		GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f);
		moved = true;
		EndMove();
	}

	public void EndMove() {
		isSelected = false;
		GameObject.Find("ValidMotion").transform.Find("go").gameObject.SetActive(false);
	}
}
