using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/**
 * Ce script sert a choisir la grille sur laquelle on travaille dans inventory controller.
 * Le même systeme est utilisé pour les slots.
 */
[RequireComponent(typeof(ItemGrid))]
public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   InventoryController ic;
   ItemGrid itemGrid;

   private void Awake() {
      ic = FindObjectOfType<InventoryController>();
      itemGrid = GetComponent<ItemGrid>();
   }

   public void OnPointerEnter(PointerEventData eventData) {
      ic.selectedItemGrid = itemGrid;
      ic.Sibling(itemGrid);
   }

   public void OnPointerExit(PointerEventData eventData) {
      ic.selectedItemGrid = null;
   }



}
