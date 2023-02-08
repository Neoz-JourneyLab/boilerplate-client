using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ExperienceBar : MonoBehaviour {
	[SerializeField] RectMask2D mask;
	static float target = 0;
	static float actual = 0;
	private static float min = 770;
	private static float max = 0;

	private void Start() {
		SetXpBar(0.2f);
	}

	public static void SetXpBar(float ratio) {
		//0 > 770
		//1 > 0
		target = ((max - min) * ratio) + min;
		GameObject.Find("Canvas").GetComponent<ExperienceBar>().StartCoroutine(nameof(BarAnim));
	}

	IEnumerator BarAnim() {
		for (float i = 0; i < 1; i += 0.01f) {
			float lerp = Mathf.Lerp(actual, target, i);
			mask.padding = new Vector4(-100, 0, lerp, 0);
			yield return new WaitForEndOfFrame();
		}
	}
}
