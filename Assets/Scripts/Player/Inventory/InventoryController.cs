using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryController : MonoBehaviour {
   public ItemGrid selectedItemGrid;

   Vector2 mousePos;

   private void Update() {
      if (selectedItemGrid == null)
         return;

      print(selectedItemGrid.GetTileGridPosition(mousePos));
   }

   void OnMousePosition(InputValue value) {
    try {
      mousePos = (Vector2)value.Get();
    } catch (Exception) {
      //ignored
    }
   }
}
