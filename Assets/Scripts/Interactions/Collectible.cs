using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Collectible : MonoBehaviour {

   public ItemAlias alias;
   public int min = 1;
   public int max = 1;

   private void Update() {
      transform.localEulerAngles = new Vector3(Mathf.Cos(Time.time * 0.15f) * 360, Mathf.Cos(Time.time * 0.12f) * 360, Mathf.Cos(Time.time * 0.1f) * 360);
   }

   private void OnTriggerEnter(Collider other) {
      if (other.tag == "Player") {
         ItemModel model = ItemCollection.GetItems().Find(i => i.alias == alias.ToString());
         var item = new InventoryItem() {
            model = model,
            quantity = Random.Range(min, max + 1)
         };
         InventoryController ic = FindObjectOfType<InventoryController>();
         var itemDispoPourStack = ic.playerItems.Find(i => i.quantity + item.quantity <= i.model.maxStack && i.model.alias == alias.ToString());
         if(itemDispoPourStack != null) {
            itemDispoPourStack.quantity += item.quantity;
            uWebSocketManager.EmitEv("update:stack", new { itemDispoPourStack.quantity, itemDispoPourStack.id });
            return;
         }
         Vector2? itemPlace = ic.playerGrid.FindSpaceForObject(item);
         if (itemPlace == null) {
            print("Trop plein");
            return;
         }
         item.onGridPosX = (int)itemPlace.Value.x;
         item.onGridPosY = (int)itemPlace.Value.y;
         FindObjectOfType<InventoryController>().playerItems.Add(item);
         uWebSocketManager.EmitEv("item:amount", new { quantity = item.quantity, model = model.id, x = (int) itemPlace.Value.x, y = (int) itemPlace.Value.y });
         //Destroy(transform.parent.gameObject);
      }
   }
}
