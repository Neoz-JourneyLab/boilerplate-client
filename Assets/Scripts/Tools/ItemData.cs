using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ItemData : ScriptableObject {
   public int width = 1;
   public int height = 1;

   public ItemCategory category = ItemCategory.ninemm;
   public int maxStack = 1;
   public int initialQuantity = 1;
   public Sprite icon;
}

public enum ItemCategory {
   Pamas, ninemm, battery
}
