using System.Linq;
using UnityEngine;

public class ContactPrefab : MonoBehaviour
{
  public string id = "";

  public void Click() {
		if (GameObject.Find("Canvas").GetComponent<MessageManager>().focus_user_id == id) {
			GameObject.Find("Canvas").GetComponent<MessageManager>().Clear();
		}

		string lastId = "null";
    if(User.conversations.ContainsKey(id) && User.conversations[id].Count == 1) {
			lastId = User.conversations[id].OrderBy(m => m.send_at).Last().id;
		}
    uWebSocketManager.EmitEv("request:messages", new { userNickname = User.users_infos[id].nickname, lastId });
		GameObject.Find("Canvas").GetComponent<MessageManager>().Focus(id);
	}
}
