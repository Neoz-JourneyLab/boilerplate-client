using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour {
   public ItemData itemData;

   public int onGridPosX;
   public int onGridPosY;

   internal void Set(ItemData itemData) {
      this.itemData = itemData;

      GetComponent<Image>().sprite = itemData.icon;

      Vector2 size = new Vector2();
      size.x = itemData.width * ItemGrid.tileSizeWidth;
      size.y = itemData.height * ItemGrid.tileSizeWidth;

      GetComponent<RectTransform>().sizeDelta = size;
   }
}
