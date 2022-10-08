using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Unit : MonoBehaviour, IPointerClickHandler {
	[SerializeField] GameObject highlight;
	[SerializeField] List<Sprite> idle;
	[SerializeField] List<Sprite> selected;
	bool isSelected = false;
	int pm = 3;

	private void Start() {
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
		print("pd");
		isSelected = true;
		for (int x = -pm; x < pm; x++) {
			for (int y = -pm; y < pm; y++) {
				foreach (var item in GameObject.FindGameObjectsWithTag("highlight")) {
					Destroy(item);
				}
				if (x + y > pm) continue;
				GameObject hl = Instantiate(highlight, this.transform);
				hl.name = "hl";
				hl.transform.position = new Vector3(transform.position.x + x, transform.position.y + y, 0);
			}
		}
	}

	public void bite() {
		print("bite");
	}

	void OnMouseDown() {
		print("mouse");
	}

	public void OnPointerClick(PointerEventData eventData) {
		print("click");
	}
}
