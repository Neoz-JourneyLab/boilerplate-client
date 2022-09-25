using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEditorInternal.Profiling.Memory.Experimental;
using Newtonsoft.Json;
using System;

/**
 * Cette classe reagit aux inputs et faits des actions dans la grille en cons�quence
 */
public class InventoryController : MonoBehaviour {
	[HideInInspector]
	public ItemGrid selectedItemGrid;
	public ItemGrid playerGrid;
	public List<InventoryItem> playerItems = new List<InventoryItem>();

	public GameObject inventoryHolder;

	[HideInInspector]
	public InventorySlot selectedSlot;

	Vector2 mousePos;

	[HideInInspector]
	public InventoryItem selectedItem;
	InventoryItem highlightedItem;
	public InventoryItem overlapItem;
	RectTransform itemRt;

	//[SerializeField] List<ItemData> items;
	[SerializeField] GameObject itemPrefab;
	[SerializeField] Transform canvasTransform;

	[HideInInspector]
	public InventoryHighlight inventoryHighlight;

	private void Awake() {
		inventoryHighlight = FindObjectOfType<InventoryHighlight>(true);
		mousePos = Vector2.zero;
	}

	private void Update() {
		if (!inventoryHolder.activeSelf)
			return;

		ItemDrag();

		if (selectedItemGrid == null) {
			inventoryHighlight.Show(false);
			return;
		}

		HandleHighlight();
	}

	public bool SynchroniseItems() {
		if (playerGrid == null)
			return false;

		bool allplaced = true;
		//On supprimes toutes les instances de l'inventaire
		for (int i = 0; i < playerGrid.transform.childCount; i++) {
			if (playerGrid.transform.GetChild(i).tag == "item") {
				Destroy(playerGrid.transform.GetChild(i).gameObject);
			}
		}

		string toPrint = "";
		foreach (InventoryItem item in playerItems) {
			// si l'objet est dans l'inventaire alors on le place
			if (item.inInventory) {
				playerGrid.CleanGridRef(item);
				CreateAndInsertItem(playerGrid, item, item.onGridPosX, item.onGridPosY);
				playerGrid.PlaceItem(item, item.prefab, item.onGridPosX, item.onGridPosY);
				toPrint += (item.model.alias + "[" + item.onGridPosX + "," + item.onGridPosY + "]\n");
			} else {
				if (!CreateAndInsertItem(playerGrid, item))
					allplaced = false;
				else {
					item.inInventory = true;
					toPrint += (item.model.alias + "[" + item.onGridPosX + "," + item.onGridPosY + "]\n");
				}
			}

		}

		return allplaced;
	}

	/**
* G�re l'affichage de la surbrillance
*/
	private void HandleHighlight() {
		Vector2Int posOnGrid = selectedItemGrid.GetTileGridPosition(mousePos);
		if (selectedItem == null) {
			highlightedItem = selectedItemGrid.GetItem(posOnGrid.x, posOnGrid.y);

			if (highlightedItem != null) {
				inventoryHighlight.Show(true);
				inventoryHighlight.SetPosition(selectedItemGrid, highlightedItem);
				inventoryHighlight.SetSize(highlightedItem);
			} else {
				inventoryHighlight.Show(false);
			}
		} else {
			inventoryHighlight.Show(selectedItemGrid.CheckBoundaries(posOnGrid.x, posOnGrid.y, selectedItem.WIDTH, selectedItem.HEIGHT));
			inventoryHighlight.SetSize(selectedItem);
			inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, posOnGrid.x, posOnGrid.y);
		}
	}

	/**
* Pose un objet a la case d�sign�e
*/
	private void PlaceItem(Vector2Int tileGridPos) {
		// si on ne peut pas placer l'item, alors on ne fait rien
		bool canPlaceItem;
		InventoryItem itemToStack;
		bool needToSwap;

		(canPlaceItem, itemToStack, needToSwap) 
			= selectedItemGrid.PlaceItem(selectedItem, selectedItem.prefab, tileGridPos.x, tileGridPos.y, ref overlapItem);

		if (!canPlaceItem)
			return;

		selectedItem.prefab.GetComponent<Image>().color = new Color(1, 1, 1, 1);

		// Si on a overlap
		if (overlapItem != null) {
			// on regarde si l'overlap �tait avec un item de m�me acabit
			if (itemToStack != null) {
				selectedItem.prefab.GetComponent<Image>().color = new Color(0.5f, 1, 1, 0.75f);
				itemRt = selectedItem.prefab.GetComponent<RectTransform>();
				selectedItem.prefab.transform.SetAsLastSibling();
				overlapItem = null;
			} else {
				playerItems.Add(selectedItem);
				var storedItem = selectedItem;
				selectedItem = overlapItem;
				playerItems.Remove(selectedItem);
				selectedItem.prefab.GetComponent<Image>().color = new Color(0.5f, 1, 1, 0.75f);
				itemRt = selectedItem.prefab.GetComponent<RectTransform>();
				selectedItem.prefab.transform.SetAsLastSibling();
				print("swap and drop : " + selectedItem.onGridPosX + ";" + selectedItem.onGridPosY + ":" + storedItem.model.alias + " ( now handle" + selectedItem.model.alias  + ")");
				overlapItem = null;
			}
		} else {
			if (itemToStack == null)
				playerItems.Add(selectedItem);
			if (needToSwap) {
				print("new item coord : " + selectedItem.onGridPosX + ";" + selectedItem.onGridPosY + " " + selectedItem.id);
				uWebSocketManager.EmitEv("item:coord", new { selectedItem.id, x = selectedItem.onGridPosX, y = selectedItem.onGridPosY });
			}
			selectedItem = null;
		}
	}

	/**
* Prends un objet a la case d�sign�e
*/
	private void PickupItem(Vector2Int tileGridPos) {
		selectedItem = selectedItemGrid.PickUpItem(tileGridPos.x, tileGridPos.y);
		print("selected " + selectedItem.id);
		uWebSocketManager.EmitEv("item:coord", new { selectedItem.id, x = -1, y = -1 });
		if (selectedItem == null)
			return;

		selectedItem.prefab.transform.SetAsLastSibling();
		itemRt = selectedItem.prefab.GetComponent<RectTransform>();
		// change item opacity when selected
		selectedItem.prefab.GetComponent<Image>().color = new Color(0.5f, 1, 1, 0.75f);
		playerItems.Remove(selectedItem);
	}

	/**
* Fait suivre notre curseur a l'objet selectionn�
*/
	void ItemDrag() {
		if (selectedItem == null)
			return;

		Vector2 itemOffset = new Vector2();
		itemOffset.x = (selectedItem.WIDTH - 1) * ItemGrid.tileSizeWidth / 2;
		itemOffset.y = -(selectedItem.HEIGHT - 1) * ItemGrid.tileSizeHeight / 2;
		itemRt.position = mousePos + itemOffset;
	}

	/**
* Creer un objet al�atoire et l'assigne en tant qu'objet selectionn�
*/
	void CreateRandomItem() {
		InventoryItem item = new InventoryItem();
		item.model = ItemCollection.GetItems()[0];
		item.quantity = 1;
		CreateAndInsertItem(playerGrid, item);
	}

	bool CreateAndInsertItem(ItemGrid grid, InventoryItem item) {
		GameObject prefab = Instantiate(itemPrefab);
		//print(JsonConvert.SerializeObject(item));
		itemRt = prefab.GetComponent<RectTransform>();
		itemRt.SetParent(canvasTransform);
		itemRt.SetAsLastSibling();

		item.Set(prefab);

		prefab.name = item.model.category.ToString();

		Vector2Int? posOnGrid = grid.FindSpaceForObject(item);
		if (posOnGrid == null) {
			return false;
		}

		grid.PlaceItem(item, prefab, posOnGrid.Value.x, posOnGrid.Value.y);
		return true;
	}

	bool CreateAndInsertItem(ItemGrid grid, InventoryItem item, int posX, int posY) {
		GameObject prefab = Instantiate(itemPrefab);
		//print(JsonConvert.SerializeObject(item));
		itemRt = prefab.GetComponent<RectTransform>();
		itemRt.SetParent(canvasTransform);
		itemRt.SetAsLastSibling();

		item.Set(prefab);

		prefab.name = item.model.category.ToString();
		try {
			grid.PlaceItem(item, prefab, posX, posY);
		} catch (Exception e) {
			print(e.Message);
			return false;
		}
		return true;
	}

	/**
* fonction utilis� pour l'affichage (change l'ordre dans la hierarchie)
*/
	public void Sibling(ItemGrid grid) {
		if (selectedItem == null)
			return;
		selectedItem.prefab.GetComponent<RectTransform>().parent = grid.GetComponent<RectTransform>();
		selectedItem.prefab.GetComponent<RectTransform>().SetAsLastSibling();
	}

	/**
* Met un objet al�atoire dans l'inventaire
*/
	void InsertRandomItem() {
		if (selectedItemGrid == null)
			return;
		CreateRandomItem();
		InventoryItem itemToInsert = selectedItem;
		InsertItem(itemToInsert);
		selectedItem = null;
	}

	/**
* Met un objet dans l'inventaire
*/
	void InsertItem(InventoryItem itemToInsert) {
		Vector2Int? posOnGrid = selectedItemGrid.FindSpaceForObject(itemToInsert);
		if (posOnGrid == null) {
			Destroy(selectedItem.prefab.gameObject);
			return;
		}

		selectedItemGrid.PlaceItem(itemToInsert, selectedItem.prefab, posOnGrid.Value.x, posOnGrid.Value.y);
	}

	/**
* Tourne un objet
*/
	void RotateItem() {
		if (selectedItem == null)
			return;

		selectedItem.Rotate();
	}

	/**
* Supprimes un objet de l'inventaire
*/
	private void ClearItemAtPos(Vector2Int tileGridPos) {
		InventoryToJson();
		/*
InventoryItem itemToClear = selectedItemGrid.itemAtPos(tileGridPos.x, tileGridPos.y);
if (itemToClear == null)
 return;

selectedItemGrid.CleanGridRef(itemToClear);
Destroy(itemToClear.gameObject);
*/
	}

	/**
* Affiche ou masque l'inventaire
*/
	private void DrawInventory(bool inventoryShow) {
		if (selectedItem != null)
			return;

		inventoryHolder.SetActive(inventoryShow);
		//obstruction.SetActive(inventoryShow);

		if (inventoryShow) {
			GameObject.FindGameObjectWithTag("Player").GetComponent<InventoryController>().SynchroniseItems();
			GetComponent<PlayerInput>().SwitchCurrentActionMap("Inventory");
		} else {
			GetComponent<PlayerInput>().SwitchCurrentActionMap("Player");
		}
	}

	/**
* Envoies un json de l'inventaire
*/
	private void InventoryToJson() {
		/*
		List<InventoryItem> itemsInInventory = new List<InventoryItem>();
		for (int i = 0; i < inventory.transform.childCount; i++) {
			 Transform currentChild = inventory.transform.GetChild(i);
			 if (currentChild.tag == "item")
					itemsInInventory.Add(currentChild.GetComponent<InventoryItem>());
		}

		string json = "";
		foreach (InventoryItem ie in itemsInInventory) {
			 json += (ie.model.category + " at pos : [" + ie.onGridPosX + ", " + ie.onGridPosY + "](" + ie.quantity + ") \n");
		}
		print(json);
		*/
	}

	#region Keybinds
	void OnMousePositionInventory(InputValue value) {
		if (value.Get() == null)
			return;
		mousePos = (Vector2)value.Get();
		/*
	mousePos.x = mouseCoords.x * 1920 / Screen.width;
	mousePos.y = mouseCoords.y * 1080 / Screen.height;
	*/
	}

	/**
* Prends ou place un objet, dans une  grille ou dans un slot
*/
	void OnClick() {
		if (selectedItemGrid == null && selectedSlot == null)
			return;

		// on place/prends un objet sur une grille
		if (selectedItemGrid != null) {
			Vector2Int tileGridPos = selectedItemGrid.GetTileGridPosition(mousePos);

			//si un objet est selectionn� alors on le place, sinon on le prends
			if (selectedItem == null)
				PickupItem(tileGridPos);
			else
				PlaceItem(tileGridPos);
		}
		// On place/prends un objet sur un slot
		else {
			if (selectedSlot.itemInSlot == null && selectedItem == null)
				return;
			if (selectedItem == null) {
				PickupSlot();
			} else if (selectedSlot.itemInSlot == null) {
				PlaceSlot();
			}
		}
	}

	void PickupSlot() {
		if (selectedItem != null)
			return;

		InventoryItem itemToPickup = selectedSlot.UnequipSlot();
		itemRt = itemToPickup.prefab.GetComponent<RectTransform>();
		selectedItem = itemToPickup;
		print("desequiping" + selectedItem.id);
	}

	void PlaceSlot() {
		if (!selectedItem.model.equipable)
			return;
		print("equiping " + selectedItem.id);
		uWebSocketManager.EmitEv("item:coord", new { selectedItem.id, x = -2, y = -2 });

		selectedSlot.EquipSlot(selectedItem);
		selectedItem = null;
	}

	void OnCheatItem() {
		/*
		string toPrint = "";
		if (selectedItem != null)
			 toPrint = "Selected item : " + selectedItem.model.alias + ".";
		if (selectedSlot != null)
			 toPrint += "Slot : " + selectedSlot + ".";
		if (selectedSlot != null && selectedSlot.itemInSlot != null)
			 toPrint += "Slot item : " + selectedSlot.itemInSlot.model.alias + ".";
		playerGrid.ViewGrid();
		print(toPrint);
		*/
		foreach (InventoryItem item in playerItems) {
			print(item.model.alias + "[" + item.onGridPosX + ", " + item.onGridPosY + "]");
		}
	}

	void OnCheatPickupItem() {
		InsertRandomItem();
	}

	void OnInventory() {
		bool inventoryShow = inventoryHolder.activeSelf;

		DrawInventory(!inventoryShow);
	}

	void OnRotate() {
		RotateItem();
	}

	void OnClear() {
		if (selectedItemGrid == null)
			return;

		Vector2Int tileGridPos = selectedItemGrid.GetTileGridPosition(mousePos);
		ClearItemAtPos(tileGridPos);
	}

	#endregion
}
