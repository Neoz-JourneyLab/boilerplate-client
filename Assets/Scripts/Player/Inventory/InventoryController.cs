using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEditorInternal.Profiling.Memory.Experimental;
using Newtonsoft.Json;

/**
 * Cette classe reagit aux inputs et faits des actions dans la grille en conséquence
 */
public class InventoryController : MonoBehaviour {
   [HideInInspector]
   public ItemGrid selectedItemGrid;
   public ItemGrid playerGrid;
   public List<InventoryItem> playerItems = new List<InventoryItem>();

   public GameObject inventoryHolder;
   public GameObject inventory;
   public GameObject obstruction;

   public InventorySlot selectedSlot;

   Vector2 mousePos;

   public InventoryItem selectedItem;
   InventoryItem highlightedItem;
   public InventoryItem overlapItem;
   RectTransform itemRt;

   //[SerializeField] List<ItemData> items;
   [SerializeField] GameObject itemPrefab;
   [SerializeField] Transform canvasTransform;

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
      foreach (InventoryItem item in playerItems) {
         if (!CreateAndInsertItem(playerGrid, item))
            allplaced = false;
      }

      return allplaced;
   }

   /**
	* Gère l'affichage de la surbrillance
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
	* Pose un objet a la case désignée
	*/
   private void PlaceItem(Vector2Int tileGridPos) {
      // si on ne peut pas placer l'item, alors on ne fait rien
      bool canPlaceItem;
      InventoryItem itemToStack;

      (canPlaceItem, itemToStack) = selectedItemGrid.PlaceItem(selectedItem, selectedItem.prefab, tileGridPos.x, tileGridPos.y, ref overlapItem);

      if (!canPlaceItem)
         return;

      selectedItem.prefab.GetComponent<Image>().color = new Color(1, 1, 1, 1);

      // Si on a overlap
      if (overlapItem != null) {
         // on regarde si l'overlap était avec un item de même acabit
         if (itemToStack != null) {
            selectedItem.prefab.GetComponent<Image>().color = new Color(0.5f, 1, 1, 0.75f);
            itemRt = selectedItem.prefab.GetComponent<RectTransform>();
            selectedItem.prefab.transform.SetAsLastSibling();
            overlapItem = null;
         } else {
            selectedItem = overlapItem;
            selectedItem.prefab.GetComponent<Image>().color = new Color(0.5f, 1, 1, 0.75f);
            itemRt = selectedItem.prefab.GetComponent<RectTransform>();
            selectedItem.prefab.transform.SetAsLastSibling();
            overlapItem = null;
         }
      } else
         selectedItem = null;
   }

   /**
	* Prends un objet a la case désignée
	*/
   private void PickupItem(Vector2Int tileGridPos) {
      selectedItem = selectedItemGrid.PickUpItem(tileGridPos.x, tileGridPos.y);
      if (selectedItem == null)
         return;

      selectedItem.prefab.transform.SetAsLastSibling();
      itemRt = selectedItem.prefab.GetComponent<RectTransform>();
      // change item opacity when selected
      selectedItem.prefab.GetComponent<Image>().color = new Color(0.5f, 1, 1, 0.75f);
   }

   /**
	* Fait suivre notre curseur a l'objet selectionné
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
	* Creer un objet aléatoire et l'assigne en tant qu'objet selectionné
	*/
   void CreateRandomItem() {
      if (selectedItem != null)
         return;

      InventoryItem item = Instantiate(itemPrefab).GetComponent<InventoryItem>();
      selectedItem = item;
      itemRt = item.prefab.GetComponent<RectTransform>();
      itemRt.SetParent(canvasTransform);
      itemRt.SetAsLastSibling();

      int selectedItemID = Random.Range(0, ItemCollection.GetItems().Count);
      //int selectedItemID = 0;
      //item.Set(ItemCollection.GetItems()[selectedItemID]);
      item.prefab.name = item.itemData.category.ToString();
   }

   bool CreateAndInsertItem(ItemGrid grid, InventoryItem item) {
      GameObject prefab = Instantiate(itemPrefab);
      //print(JsonConvert.SerializeObject(item));
      itemRt = prefab.GetComponent<RectTransform>();
      itemRt.SetParent(canvasTransform);
      itemRt.SetAsLastSibling();

      item.Set(prefab);

      prefab.name = item.itemData.category.ToString();

      Vector2Int? posOnGrid = grid.FindSpaceForObject(item);
      if (posOnGrid == null) {
         return false;
      }

      grid.PlaceItem(item, prefab, posOnGrid.Value.x, posOnGrid.Value.y);
      return true;
   }

   /**
	* fonction utilisé pour l'affichage (change l'ordre dans la hierarchie)
	*/
   public void Sibling(ItemGrid grid) {
      if (selectedItem == null)
         return;
      selectedItem.prefab.GetComponent<RectTransform>().parent = grid.GetComponent<RectTransform>();
      selectedItem.prefab.GetComponent<RectTransform>().SetAsLastSibling();
   }

   /**
	* Met un objet aléatoire dans l'inventaire
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
      inventoryHolder.SetActive(inventoryShow);
      //obstruction.SetActive(inventoryShow);

      if (inventoryShow) {
         GetComponent<PlayerInput>().SwitchCurrentActionMap("Inventory");
      } else {
         GetComponent<PlayerInput>().SwitchCurrentActionMap("Player");
      }
   }

   /**
	* Envoies un json de l'inventaire
	*/
   private void InventoryToJson() {
      List<InventoryItem> itemsInInventory = new List<InventoryItem>();
      for (int i = 0; i < inventory.transform.childCount; i++) {
         Transform currentChild = inventory.transform.GetChild(i);
         if (currentChild.tag == "item")
            itemsInInventory.Add(currentChild.GetComponent<InventoryItem>());
      }

      string json = "";
      foreach (InventoryItem ie in itemsInInventory) {
         json += (ie.itemData.category + " at pos : [" + ie.onGridPosX + ", " + ie.onGridPosY + "](" + ie.quantity + ") \n");
      }
      print(json);

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

         //si un objet est selectionné alors on le place, sinon on le prends
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
      selectedItem = selectedSlot.UnequipSlot(); ;
   }

   void PlaceSlot() {
      if (!selectedItem.itemData.equipable)
         return;

      selectedSlot.EquipSlot(selectedItem);
      selectedItem = null;
   }

   void OnCheatItem() {
      if (selectedItem == null)
         CreateRandomItem();
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
