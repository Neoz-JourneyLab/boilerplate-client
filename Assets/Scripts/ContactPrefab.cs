using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContactPrefab : MonoBehaviour
{
  public string id;

  public void Click() {
    GameObject.Find("Canvas").GetComponent<MessageManager>().Focus(id);
  }
}
