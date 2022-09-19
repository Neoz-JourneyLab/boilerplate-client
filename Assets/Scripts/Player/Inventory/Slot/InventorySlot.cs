using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   public InventoryController ic;

   public int maxWidth;
   public int maxHeight;

   public InventoryItem itemInSlot;

   public Gun pamas;

   InventorySlot slot;
   GunControl gc;

   void Start() {
      slot = GetComponent<InventorySlot>();
      gc = FindObjectOfType<GunControl>();
   }

   public void EquipSlot(InventoryItem itemToEquip) {
      if (itemInSlot != null)
         return;

      if (itemToEquip.itemData.category == ItemCategory.Pamas)
         gc.EquipGun(pamas);

      itemInSlot = itemToEquip;
   }

   public InventoryItem UnequipSlot() {
      InventoryItem toReturn = itemInSlot;
      itemInSlot = null;
      gc.UnequipGun();
      return toReturn;
   }

   public void OnPointerEnter(PointerEventData eventData) {
      ic.selectedSlot = slot;
   }

   public void OnPointerExit(PointerEventData eventData) {
      ic.selectedSlot = null;
   }
}
