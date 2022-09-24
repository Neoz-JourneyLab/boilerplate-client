using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/**
 * Ce script sert a choisir le slot sur lequel on travaille dans inventory controller.
 * Le même systeme est utilisé pour les grilles.
 */
public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   public InventoryController ic;

   public int maxWidth;
   public int maxHeight;

   public InventoryItem itemInSlot;

   public Gun pamas;

   InventorySlot slot;
   GunControl gc;

   RectTransform rt;

   void Start() {
      rt = GetComponent<RectTransform>();
      slot = GetComponent<InventorySlot>();
      gc = FindObjectOfType<GunControl>();
   }

   public void EquipSlot(InventoryItem itemToEquip) {
      if (itemInSlot != null)
         return;

      Vector2 pos = new Vector2();
      pos.x += itemToEquip.WIDTH * ItemGrid.tileSizeWidth / 2;
      pos.y -= itemToEquip.HEIGHT * ItemGrid.tileSizeHeight / 2;

      if (itemToEquip.itemModel.category == ItemCategory.weapon.ToString())
         gc.EquipGun(pamas, itemToEquip.weaponData.currentAmmo);

      itemInSlot = itemToEquip;
      itemToEquip.prefab.transform.parent = transform;
      itemToEquip.prefab.transform.localPosition = pos;
   }

   public InventoryItem UnequipSlot() {
      InventoryItem toReturn = itemInSlot;
      itemInSlot = null;
      toReturn.weaponData.currentAmmo = gc.UnequipGunAmmo();
      return toReturn;
   }

   public void OnPointerEnter(PointerEventData eventData) {
      ic.selectedSlot = slot;
   }

   public void OnPointerExit(PointerEventData eventData) {
      ic.selectedSlot = null;
   }
}
