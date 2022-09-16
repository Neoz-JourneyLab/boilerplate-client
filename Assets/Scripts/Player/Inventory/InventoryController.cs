using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryController : MonoBehaviour {
   [HideInInspector]
   public ItemGrid selectedItemGrid;

   Vector2 mousePos;

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
      print(selectedItemGrid.GetTileGridPosition(mousePos));
   }
}
