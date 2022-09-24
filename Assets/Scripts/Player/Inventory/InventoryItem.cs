using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
 * Contient les donn�es propres aux objets a leur creation.
 */
public class InventoryItem {
	public itemModel itemModel;
	public int quantity;
	public WeaponData weaponData = null;
	public GameObject prefab = null;
	public int HEIGHT {
		get {
			if (!rotated)
				return itemModel.height;
			return itemModel.width;
		}
	}

	public int WIDTH {
		get {
			if (!rotated)
				return itemModel.width;
			return itemModel.height;
		}
	}

	public int onGridPosX;
	public int onGridPosY;

	public bool rotated = false;

	RectTransform rt;
	TextMeshProUGUI quantityText;

	internal void Set(GameObject itemPrefab) {
		prefab = itemPrefab;

		quantityText = prefab.GetComponentInChildren<TextMeshProUGUI>();
		prefab.GetComponent<Image>().sprite = itemModel.icon;

		Vector2 size = new Vector2();
		size.x = WIDTH * ItemGrid.tileSizeWidth;
		size.y = HEIGHT * ItemGrid.tileSizeWidth;

		prefab.GetComponent<RectTransform>().sizeDelta = size;
		rt = prefab.GetComponent<RectTransform>();

		if (itemModel.category == ItemCategory.weapon.ToString())
			weaponData = new WeaponData(0);

		prefab.transform.localScale = Vector3.one;
		UpdateQuantity();
	}

	internal void Rotate() {
		rotated = !rotated;

		rt.rotation = Quaternion.Euler(0, 0, rotated ? 90f : 0f);
	}

	public void UpdateQuantity() {
		quantityText.text = quantity.ToString();
	}
}

public class WeaponData {
	public int currentAmmo;

	public WeaponData(int ammo) {
		currentAmmo = ammo;
	}
}
