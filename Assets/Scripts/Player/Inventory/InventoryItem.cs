using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour {
   public ItemData itemData;
   public int quantity;

   public int HEIGHT {
      get {
         if (!rotated)
            return itemData.height;
         return itemData.width;
      }
   }

   public int WIDTH {
      get {
         if (!rotated)
            return itemData.width;
         return itemData.height;
      }
   }

   public int onGridPosX;
   public int onGridPosY;

   public bool rotated = false;

   RectTransform rt;
   TextMeshProUGUI quantityText;

   internal void Set(ItemData itemData) {
      this.itemData = itemData;
      this.quantity = itemData.initialQuantity;
      this.quantityText = GetComponentInChildren<TextMeshProUGUI>();

      GetComponent<Image>().sprite = itemData.icon;

      Vector2 size = new Vector2();
      size.x = WIDTH * ItemGrid.tileSizeWidth;
      size.y = HEIGHT * ItemGrid.tileSizeWidth;

      GetComponent<RectTransform>().sizeDelta = size;
      rt = GetComponent<RectTransform>();

      UpdateQuantity();
   }

   internal void Rotate() {
      rotated = !rotated;

      rt.rotation = Quaternion.Euler(0, 0, rotated ? 90f : 0f);
   }

   public void UpdateQuantity() {
      quantityText.text = quantity.ToString();
   }
}
