using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContactPrefab : MonoBehaviour
{
  public string id = "";

  public void Click() {
		if (GameObject.Find("Canvas").GetComponent<MessageManager>().focus_user_id == id) {
			GameObject.Find("Canvas").GetComponent<MessageManager>().Clear();
			return;
		}
		foreach (Transform item in transform.parent) {
			if (item.name == name) item.GetComponent<Image>().color = new Color(.18f, .22f, .64f);
			else item.GetComponent<Image>().color = new Color(.13f, .18f, .3f);
		}
		transform.Find("nick").GetComponent<TMP_Text>().color = new Color(0, .77f, 1f);
		GameObject.Find("Canvas").GetComponent<MessageManager>().Focus(id);
	}
}
