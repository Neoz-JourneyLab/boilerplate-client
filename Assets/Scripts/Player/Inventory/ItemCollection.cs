using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
 * Ceci stockes les différents items en attendant la BDD
 */
public static class ItemCollection {
   static List<ItemModel> items = new ();
   public static List<ItemModel> GetItems() {      
      return items;
   }

   public static void Set(ItemModel item) {
      items.Add(item);
   }
}
