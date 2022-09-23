using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ItemData {
   public int width = 1;
   public int height = 1;

   public string category = "";
   public string alias = "";
   public int maxStack = 1;
   public int initialQuantity = 1;
   public bool equipable = false;
   public Sprite icon;

  public void SetIcon() {
    icon = Resources.Load<Sprite>(alias);
  }
}

public enum ItemCategory {
   weapon, ammo, battery
}

public enum ItemAlias {
  pamas
}
