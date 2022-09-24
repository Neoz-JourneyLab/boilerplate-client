using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
 * Ceci stockes les différents items en attendant la BDD
 */
public static class ItemCollection {
   static List<itemModel> items;
   public static List<itemModel> GetItems() {
      if(items == null) {
         items = new List<itemModel>() {
      new itemModel() {
         alias = "pamas",
         category = ItemCategory.weapon.ToString(),
         equipable = true,
         height = 2,
         width = 2,
         id = "blabla",
         maxStack = 1
      }
   };
         items.Last().SetIcon();
      }
      return items;
   }

   public static void Set(itemModel item) {
      items.Add(item);
   }
}
