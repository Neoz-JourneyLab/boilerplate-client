using UnityEngine;

public class SwapGo : MonoBehaviour
{
  [SerializeField] GameObject[] items;

  public void Click() {
    foreach (var item in items) {
      item.SetActive(!item.activeSelf);
    }
  }
}
