using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryController : MonoBehaviour {
   [HideInInspector]
   public ItemGrid selectedItemGrid;

   Vector2 mousePos;

   InventoryItem selectedItem;

   private void Update() {

   }
   void OnMousePosition(InputValue value) {
      if (value.Get() == null)
         return;
      mousePos = (Vector2)value.Get();
   }

   void OnShoot() {
      if (selectedItemGrid == null)
         return;

      Vector2Int tileGridPos = selectedItemGrid.GetTileGridPosition(mousePos);
      if (selectedItem == null) {
         selectedItem = selectedItemGrid.PickUpItem(tileGridPos.x, tileGridPos.y);
      } else {
         selectedItemGrid.PlaceItem(selectedItem, tileGridPos.x, tileGridPos.y);
         selectedItem = null;
      }
   }


}
