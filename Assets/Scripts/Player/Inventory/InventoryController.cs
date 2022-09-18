using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;


public class InventoryController : MonoBehaviour {
   [HideInInspector]
   public ItemGrid selectedItemGrid;

   public GameObject inventoryHolder;
   public GameObject inventory;
   public GameObject obstruction;

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

      OnCheatPickupItem();
      OnCheatPickupItem();
      OnCheatPickupItem();
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

   private void PlaceItem(Vector2Int tileGridPos) {
      // si on ne peut pas placer l'item, alors on ne fait rien
      bool canPlaceItem;
      InventoryItem itemToStack;

      (canPlaceItem, itemToStack) = selectedItemGrid.PlaceItem(selectedItem, tileGridPos.x, tileGridPos.y, ref overlapItem);

      if (!canPlaceItem)
         return;

      selectedItem.GetComponent<Image>().color = new Color(1, 1, 1, 1);

      // Si on a overlap
      if (overlapItem != null) {
         // on regarde si l'overlap était avec un item de même acabit
         if (itemToStack != null) {
            selectedItem.GetComponent<Image>().color = new Color(0.5f, 1, 1, 0.75f);
            itemRt = selectedItem.GetComponent<RectTransform>();
            selectedItem.transform.SetAsLastSibling();
            overlapItem = null;
         } else {
            selectedItem = overlapItem;
            selectedItem.GetComponent<Image>().color = new Color(0.5f, 1, 1, 0.75f);
            itemRt = selectedItem.GetComponent<RectTransform>();
            selectedItem.transform.SetAsLastSibling();
            overlapItem = null;
         }
      } else
         selectedItem = null;
   }

   private void PickupItem(Vector2Int tileGridPos) {
      selectedItem = selectedItemGrid.PickUpItem(tileGridPos.x, tileGridPos.y);
      if (selectedItem == null)
         return;

      selectedItem.transform.SetAsLastSibling();
      itemRt = selectedItem.GetComponent<RectTransform>();
      // change item opacity when selected
      selectedItem.GetComponent<Image>().color = new Color(0.5f, 1, 1, 0.75f);
   }

   void ItemDrag() {
      if (selectedItem != null) {
         Vector2 itemOffset = new Vector2();
         itemOffset.x = (selectedItem.WIDTH - 1) * ItemGrid.tileSizeWidth / 2;
         itemOffset.y = -(selectedItem.HEIGHT - 1) * ItemGrid.tileSizeHeight / 2;
         itemRt.position = mousePos + itemOffset;
      }

   }

   void CreateRandomItem() {
      if (selectedItem != null)
         return;
      InventoryItem item = Instantiate(itemPrefab).GetComponent<InventoryItem>();
      selectedItem = item;
      itemRt = item.GetComponent<RectTransform>();
      itemRt.SetParent(canvasTransform);
      itemRt.SetAsLastSibling();

      int selectedItemID = Random.Range(0, FindObjectOfType<ItemCollection>().items.Count);
      //int selectedItemID = 0;
      item.Set(FindObjectOfType<ItemCollection>().items[selectedItemID]);
      item.name = item.itemData.name;
   }

   public void Sibling(ItemGrid grid) {
      if (!selectedItem)
         return;
      selectedItem.GetComponent<RectTransform>().parent = grid.GetComponent<RectTransform>();
      selectedItem.GetComponent<RectTransform>().SetAsLastSibling();
   }

   void InsertRandomItem() {
      if (selectedItemGrid == null)
         return;
      CreateRandomItem();
      InventoryItem itemToInsert = selectedItem;
      InsertItem(itemToInsert);
      selectedItem = null;
   }

   void InsertItem(InventoryItem itemToInsert) {
      Vector2Int? posOnGrid = selectedItemGrid.FindSpaceForObject(itemToInsert);
      if (posOnGrid == null) {
         Destroy(selectedItem.gameObject);
         return;
      }

      selectedItemGrid.PlaceItem(itemToInsert, posOnGrid.Value.x, posOnGrid.Value.y);
   }

   void RotateItem() {
      if (selectedItem == null)
         return;

      selectedItem.Rotate();
   }

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

   private void DrawInventory(bool inventoryShow) {
      inventoryHolder.SetActive(inventoryShow);
      //obstruction.SetActive(inventoryShow);

      if (inventoryShow) {
         GetComponent<PlayerInput>().SwitchCurrentActionMap("Inventory");
      } else {
         GetComponent<PlayerInput>().SwitchCurrentActionMap("Player");
      }
   }

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
   }

   void OnClick() {
      if (selectedItemGrid == null)
         return;

      Vector2Int tileGridPos = selectedItemGrid.GetTileGridPosition(mousePos);

      //if we have an item selected, we place it. Else we take the item on the grid.

      if (selectedItem == null)
         PickupItem(tileGridPos);
      else
         PlaceItem(tileGridPos);
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
