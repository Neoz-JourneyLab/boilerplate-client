using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OpacityTextVariation : MonoBehaviour {
	[SerializeField] TMP_Text txt;
	[SerializeField] Image img;
	[SerializeField] float min = 0;
	[SerializeField] float max = 1;
	[SerializeField] float speed = 1;

	private void Awake() {
		if (txt == null && img == null) txt = GetComponent<TMP_Text>();
		if (txt == null && img == null) txt = GetComponentInChildren<TMP_Text>();
		if (txt == null && img == null) img = GetComponentInChildren<Image>();
		if (img == null && txt == null) {
			print("Erreur Opacity : " + this.name);
		}
	}

	private void Update() {
		if (txt == null) {
			img.color = new Color(img.color.r, img.color.g, img.color.b, ((Mathf.Sin(Time.time * speed) + 1) / 2f) * (min - max) + max);
		} else {
			txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, ((Mathf.Sin(Time.time * speed) + 1) / 2f) * (min - max) + max);
		}
	}
}
