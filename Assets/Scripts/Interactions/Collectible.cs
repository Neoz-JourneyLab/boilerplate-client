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
			ItemData model = ItemCollection.GetItem(alias.ToString());
			var item = new InventoryItem() {
				itemData = model,
				quantity = Random.Range(min, max + 1)
			};
			FindObjectOfType<InventoryController>().playerItems.Add(item);
			uWebSocketManager.EmitEv("item:amount", new { quantity = item.quantity, model = model.id });
			Destroy(transform.parent.gameObject);
		}
	}
}
