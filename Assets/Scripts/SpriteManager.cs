using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SpriteManager {
  static Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

  public static Sprite GetSprite(string k) {
    if (!sprites.ContainsKey(k)) {
      try {
        Sprite sprite = Resources.Load<Sprite>("sprites/" + k);
        if (sprite == null) sprite = GetSprite("error");
        sprites[k] = sprite;
      } catch (Exception) {
        sprites[k] = GetSprite("error");
      }
    }
    return sprites[k];
  }
}
