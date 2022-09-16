using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemGrid : MonoBehaviour {
   const float tileSize = 100;

   RectTransform rt;

   Vector2 positionOnGrid = new Vector2();
   Vector2Int tileGridPos = new Vector2Int();

   InventoryItem[,] inventoryItemSlot;

   [SerializeField] int gridSizeWidth;
   [SerializeField] int gridSizeHeight;

   [SerializeField] GameObject inventoryItemPrefab;

   public void Start() {
      rt = GetComponent<RectTransform>();
      Init(gridSizeWidth, gridSizeHeight);

      InventoryItem ie = Instantiate(inventoryItemPrefab.GetComponent<InventoryItem>());
      PlaceItem(ie, 3, 6);
   }

   private void Init(int width, int height) {
      inventoryItemSlot = new InventoryItem[width, height];
      Vector2 size = new Vector2(width * tileSize, height * tileSize);
      rt.sizeDelta = size;
   }


   public Vector2Int GetTileGridPosition(Vector2 mousePos) {
      positionOnGrid.x = mousePos.x - rt.position.x;
      positionOnGrid.y = rt.position.y - mousePos.y;

      tileGridPos.x = (int)(positionOnGrid.x / tileSize);
      tileGridPos.y = (int)(positionOnGrid.y / tileSize);

      return tileGridPos;
   }

   public void PlaceItem(InventoryItem item, int x, int y) {
      RectTransform itemRt = item.GetComponent<RectTransform>();
      itemRt.SetParent(rt);
      inventoryItemSlot[x, y] = item;

      Vector2 position = new Vector2();
      position.x = x * tileSize + tileSize / 2;
      position.y = -(y * tileSize + tileSize / 2);

      itemRt.localPosition = position;
   }

   public InventoryItem PickUpItem(int x, int y) {
      InventoryItem toReturn = inventoryItemSlot[x, y];
      inventoryItemSlot[x, y] = null;
      return toReturn;
   }

}
