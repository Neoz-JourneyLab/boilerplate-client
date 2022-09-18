using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemGrid : MonoBehaviour {
   public const float tileSizeHeight = 100;
   public const float tileSizeWidth = 100;
   RectTransform rt;

   Vector2 positionOnGrid = new Vector2();
   Vector2Int tileGridPos = new Vector2Int();

   InventoryItem[,] inventoryItemSlot;

   [SerializeField] int gridSizeWidth;
   [SerializeField] int gridSizeHeight;

   public void Start() {
      rt = GetComponent<RectTransform>();
      Init(gridSizeWidth, gridSizeHeight);
   }

   private void Init(int width, int height) {
      inventoryItemSlot = new InventoryItem[width, height];
      Vector2 size = new Vector2(width * tileSizeWidth, height * tileSizeHeight);
      rt.sizeDelta = size;
   }

   #region ItemMethods 
   internal InventoryItem GetItem(int x, int y) {
      if (!CheckInGrid(x, y))
         return null;

      return inventoryItemSlot[x, y];
   }

   public bool PlaceItem(InventoryItem item, int x, int y, ref InventoryItem overlapItem) {
      // On check si l'item sortira de la grille si on le place ici, si c'est le cas, on ne fait rien
      // (donc on renvoie false)
      if (!CheckBoundaries(x, y, item.WIDTH, item.HEIGHT))
         return false;

      if (!OverlapCheck(x, y, item.WIDTH, item.HEIGHT, ref overlapItem)) {
         overlapItem = null;
         return false;
      }

      if (overlapItem != null)
         CleanGridRef(overlapItem);

      return PlaceItem(item, x, y);
   }

   public bool PlaceItem(InventoryItem item, int x, int y) {
      RectTransform itemRt = item.GetComponent<RectTransform>();
      itemRt.SetParent(rt);

      item.onGridPosX = x;
      item.onGridPosY = y;

      for (int i = 0; i < item.WIDTH; i++) {
         for (int j = 0; j < item.HEIGHT; j++) {
            inventoryItemSlot[x + i, y + j] = item;
         }
      }

      Vector2 position = CalculatePositionOnGrid(item, x, y);

      itemRt.localPosition = position;
      return true;
   }

   public InventoryItem PickUpItem(int x, int y) {
      // On regarde si on clique dans la grille
      if (!CheckInGrid(x, y))
         return null;

      // Si oui on regarde si on cliques sur un objet
      InventoryItem toReturn = inventoryItemSlot[x, y];
      if (toReturn == null)
         return null;

      // On vides l'objet dans la grille (on met a null toutes les cases qu'il occupait)
      CleanGridRef(toReturn);

      // et on le return
      return toReturn;
   }

   private void CleanGridRef(InventoryItem item) {
      for (int i = 0; i < item.WIDTH; i++) {
         for (int j = 0; j < item.HEIGHT; j++) {
            inventoryItemSlot[item.onGridPosX + i, item.onGridPosY + j] = null;
         }
      }

   }

   #endregion

   #region Calculus Tools
   public Vector2Int GetTileGridPosition(Vector2 mousePos) {
      positionOnGrid.x = mousePos.x - rt.position.x;
      positionOnGrid.y = rt.position.y - mousePos.y;

      tileGridPos.x = (int)(positionOnGrid.x / tileSizeWidth);
      tileGridPos.y = (int)(positionOnGrid.y / tileSizeHeight);

      return tileGridPos;
   }

   public Vector2 CalculatePositionOnGrid(InventoryItem item, int x, int y) {
      Vector2 position = new Vector2();
      position.x = x * tileSizeWidth + tileSizeWidth * item.WIDTH / 2;
      position.y = -(y * tileSizeHeight + tileSizeHeight * item.HEIGHT / 2);
      return position;
   }

   public bool CheckInGrid(int x, int y) {
      return !(x >= gridSizeWidth || x < 0 || y < 0 || y >= gridSizeHeight);
   }

   public bool CheckBoundaries(int x, int y, int width, int height) {
      if (!CheckInGrid(x, y))
         return false;
      if (!CheckInGrid(x + width - 1, y + height - 1))
         return false;

      return true;
   }

   private bool OverlapCheck(int x, int y, int width, int height, ref InventoryItem overlapItem) {
      for (int i = 0; i < width; i++) {
         for (int j = 0; j < height; j++) {
            if (inventoryItemSlot[x + i, y + j] == null)
               continue;
            if (overlapItem == null) {
               overlapItem = inventoryItemSlot[x + i, y + j];
            } else {
               if (overlapItem != inventoryItemSlot[x + i, y + j]) {
                  return false;
               }
            }
         }
      }

      return true;
   }

   private bool CheckAvailableSpace(int x, int y, int width, int height) {
      for (int i = 0; i < width; i++) {
         for (int j = 0; j < height; j++) {
            if (inventoryItemSlot[x + i, y + j] == null)
               continue;
            return false;
         }
      }
      return true;
   }

   private void ViewGrid() {
      String toPrint = "";
      for (int i = 0; i < gridSizeWidth; i++) {
         for (int j = 0; j < gridSizeHeight; j++) {
            if (inventoryItemSlot[i, j] != null)
               toPrint += ("[" + i + "," + j + "] = " + inventoryItemSlot[i, j] + ".");
         }
         toPrint += "\n";
      }

      print(toPrint);
   }

   internal Vector2Int? FindSpaceForObject(InventoryItem item) {
      int width = gridSizeWidth - (item.WIDTH - 1);
      int height = gridSizeHeight - (item.HEIGHT - 1);
      for (int i = 0; i < width; i++) {
         for (int j = 0; j < height; j++) {
            if (CheckAvailableSpace(i, j, item.WIDTH, item.HEIGHT)) {
               return new Vector2Int(i, j);
            }
         }
      }

      return null;
   }
   #endregion
}
