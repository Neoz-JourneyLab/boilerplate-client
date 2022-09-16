using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour {
   public GameObject inventory;
   // Start is called before the first frame update
   void Start() {

   }

   // Update is called once per frame
   void Update() {

   }

   void OnInventory() {
      if (inventory == null)
         return;
      inventory.SetActive(!inventory.activeSelf);
   }
}
