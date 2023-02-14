using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gameobject instatiated in the scroll list of current conversations
/// </summary>
public class ContactPrefab : MonoBehaviour {
	public string id = ""; //assigneg by the instantier
	bool requested = false;

	public void Click() {
		//if click on already focused, close the convo tab
		if (GameObject.Find("Canvas").GetComponent<MessageManager>().focus_user_id == id) {
			GameObject.Find("Canvas").GetComponent<MessageManager>().Clear();
			return;
		}

		//else, highlight the frame
		foreach (Transform item in transform.parent) {
			if (item.name == name) item.GetComponent<Image>().color = new Color(.18f, .22f, .64f);
			else item.GetComponent<Image>().color = new Color(.13f, .18f, .3f);
		}

		if (!requested) {
			uWebSocketManager.EmitEv("request:messages", new { userId = id, lastId = User.conversations[id].First().id, limit = 10 });
			requested = true;
		}

		//reset nick color (highlighted if new message and not in focus)
		transform.Find("nick").GetComponent<TMP_Text>().color = new Color(0, .77f, 1f);
		GameObject.Find("Canvas").GetComponent<MessageManager>().Focus(id);
	}
}
