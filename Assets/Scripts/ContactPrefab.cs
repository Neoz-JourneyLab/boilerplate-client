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
		}

		string lastId = "null";
    if(User.conversations.ContainsKey(id)) {
			lastId = User.conversations[id].OrderBy(m => m.send_at).First().id;
		}
		if(!WsEvents.noPreviousMessageFromThosesUsers.Contains(id)) {
			uWebSocketManager.EmitEv("request:messages", new { userId = id, lastId });
		}
		transform.Find("nick").GetComponent<TMP_Text>().color = new Color(0, .77f, 1f);

		foreach (Transform item in transform.parent) {
			if (item.name == name) item.GetComponent<Image>().color = new Color(.18f, .22f, .64f);
			else item.GetComponent<Image>().color = new Color(.13f, .18f, .3f);
		}

		GameObject.Find("Canvas").GetComponent<MessageManager>().Focus(id);
	}
}
