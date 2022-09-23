using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Ceci stockes les différents items en attendant la BDD
 */
public static class ItemCollection {
	static List<ItemData> items = new List<ItemData>();
	public static List<ItemData> GetItems() {
		return items;
	}

	public static void Set(ItemData item) {
		items.Add(item);
	}
}
