using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	/// <summary>
	/// récupère un modèle d'objet par ID ou par ALIAS
	/// </summary>
	/// <param name="idOrAlias"></param>
	public static ItemData GetItem(string idOrAlias) {
		if (idOrAlias.StartsWith("0000")) return items.First(i => i.id == idOrAlias);
		return items.First(i => i.alias == idOrAlias);
	}
}
