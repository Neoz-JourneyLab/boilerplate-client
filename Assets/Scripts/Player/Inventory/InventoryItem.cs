using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
 * Contient les données propres aux objets a leur creation.
 */

public class InventoryItem {
   public ItemModel model;
   public int quantity;
   public string id;
   public WeaponData weaponData = null;
   public GameObject prefab = null;
   public bool inInventory;
   public int HEIGHT {
      get {
         if (!rotated)
            return model.height;
         return model.width;
      }
   }

   public int WIDTH {
      get {
         if (!rotated)
            return model.width;
         return model.height;
      }
   }

   public int onGridPosX;
   public int onGridPosY;

   public bool rotated = false;

   RectTransform rt;
   TextMeshProUGUI quantityText;

   /// <summary>
   /// place la forme de la prefab, son sprite, etc
   /// </summary>
   internal void Set(GameObject itemPrefab) {
      prefab = itemPrefab;

      quantityText = prefab.GetComponentInChildren<TextMeshProUGUI>();
      prefab.GetComponent<Image>().sprite = model.icon;

      Vector2 size = new Vector2();
      size.x = WIDTH * ItemGrid.tileSizeWidth;
      size.y = HEIGHT * ItemGrid.tileSizeWidth;

      prefab.GetComponent<RectTransform>().sizeDelta = size;
      rt = prefab.GetComponent<RectTransform>();

      if (model.category == ItemCategory.weapon.ToString())
         weaponData = new WeaponData(0);

      prefab.transform.localScale = Vector3.one;
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

public class WeaponData {
   public int currentAmmo;

   public WeaponData(int ammo) {
      currentAmmo = ammo;
   }
}
