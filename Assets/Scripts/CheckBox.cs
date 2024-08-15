using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CheckBox : MonoBehaviour {

  [SerializeField] bool state = false;
  Image img;

  private void Start() {
    img = GetComponent<Image>();
    SetSprite();
  }

  public void Click() {
    state = !state;
    SetSprite();
  }

  public void Set(bool _state) {
    state = _state;
    SetSprite();
  }

  void SetSprite() {
    img.sprite = SpriteManager.GetSprite(state ? "misc/checked" : "misc/unchecked");
  }
}
