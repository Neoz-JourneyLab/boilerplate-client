using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Placement les objets dans la grille d'inventaire
 */
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
      if (name == "PlayerInventory") {
         GameObject.FindGameObjectWithTag("Player").GetComponent<InventoryController>().SynchroniseItems();
      }
   }

   /**
	 * Init crée un nouvelle grille de taille width, length
	 */
   private void Init(int width, int height) {
      inventoryItemSlot = new InventoryItem[width, height];
      Vector2 size = new Vector2(width * tileSizeWidth, height * tileSizeHeight);
      rt.sizeDelta = size;
   }

   #region ItemMethods 
   /**
	 * renvoies l'objet au coordonnées [x,y]
	 */
   internal InventoryItem GetItem(int x, int y) {
      if (!CheckInGrid(x, y))
         return null;
      return inventoryItemSlot[x, y];
   }

   /**
	 * Place un objet dans la grille, renvoies un booléen (indiquant si l'objet a été placé), et un Inventory Item dans
	 * les cas ou l'item est stacké mais que le stack maximum a été atteint
	 */
   public (bool, InventoryItem) PlaceItem(InventoryItem item, GameObject prefab, int x, int y, ref InventoryItem overlapItem) {
      // On check si l'item sortira de la grille si on le place ici, si c'est le cas, on ne fait rien
      // (donc on renvoie false)
      if (!CheckBoundaries(x, y, item.WIDTH, item.HEIGHT))
         return (false, null);

      // Si l'objet overlap avec plus de 2 objets, alors on n'interverti pas
      if (!OverlapCheck(x, y, item.WIDTH, item.HEIGHT, ref overlapItem)) {
         overlapItem = null;
         return (false, null);
      }

      if (overlapItem != null) {
         //si on overlap mais sur un objet qui est stackable, on additionne a l'objet stacké ce qu'il faut
         //et on garde le surplus selectionné
         InventoryItem itemToReturn = CheckOverlapQuantity(item, ref overlapItem);
         if (itemToReturn != null)
            return (true, itemToReturn);

         //Sinon  on clear l'item de la grille
         CleanGridRef(overlapItem);
      }

      // et on interverti les objets (ou place simplement)
      return (PlaceItem(item, prefab, x, y), null);
   }

   /**
	 * Place un objet dans la grille sans vérifications (fait au préalable)
	 */
   public bool PlaceItem(InventoryItem item, GameObject prefab, int x, int y) {
      RectTransform itemRt = prefab.GetComponent<RectTransform>();
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

   /**
	 * La fonction sert lors du stacking d'objets, elle renvoies l'item qui restera selectionné
	 */
   private InventoryItem CheckOverlapQuantity(InventoryItem item, ref InventoryItem overlapItem) {
      // On regarde toutes les conditions qui rendent le stack impossible
      if (item.itemData.category != overlapItem.itemData.category) // meme item
         return null;
      if (overlapItem.itemData.maxStack <= 1) // pas stackable
         return null;
      if (overlapItem.quantity == overlapItem.itemData.maxStack) // trop plein
         return null;

      ItemData oldata = overlapItem.itemData;

      overlapItem.quantity += item.quantity;

      // si on met le maximum sur le slot "overlap"
      if (overlapItem.quantity > oldata.maxStack) {
         // On retire ce qu'on a mis a l'objet selectionné mais il reste selectionné
         item.quantity = overlapItem.quantity - oldata.maxStack;
         overlapItem.quantity = oldata.maxStack;
         overlapItem.UpdateQuantity();
         item.UpdateQuantity();
      } else {
         // Sinon si on a tout stacké nous n'avons plus d'objet selectionné.
         Destroy(item.prefab.gameObject);
         overlapItem.UpdateQuantity();
         overlapItem = null;
      }

      return item;
   }

   /**
	 * On renvoies l'objet au coordonnées passées en parametre et le renvoies
	 */
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

   /**
	 * Supprimes dans le tableau un item (lors d'une selection par exemple)
	 */
   public void CleanGridRef(InventoryItem item) {
      for (int i = 0; i < item.WIDTH; i++) {
         for (int j = 0; j < item.HEIGHT; j++) {
            inventoryItemSlot[item.onGridPosX + i, item.onGridPosY + j] = null;
         }
      }
   }

   #endregion

   #region Calculus Tools

   /**
	 * Donne la case selectionnée dans la grille avec la souris
	 */
   public Vector2Int GetTileGridPosition(Vector2 mousePos) {
      positionOnGrid.x = (mousePos.x - rt.position.x) * 1920 / Screen.width;
      positionOnGrid.y = (rt.position.y - mousePos.y) * 1080 / Screen.height;

      tileGridPos.x = (int)(positionOnGrid.x / tileSizeWidth);
      tileGridPos.y = (int)(positionOnGrid.y / tileSizeHeight);
      return tileGridPos;
   }

   /**
	 * Calcule la position pour que le sprite soit bien placé 
	 */
   public Vector2 CalculatePositionOnGrid(InventoryItem item, int x, int y) {
      Vector2 position = new Vector2();
      position.x = x * tileSizeWidth + tileSizeWidth * item.WIDTH / 2;
      position.y = -(y * tileSizeHeight + tileSizeHeight * item.HEIGHT / 2);
      return position;
   }

   /**
	 * Verifies que les parametres décrivent bien une case de la grille
	 */
   public bool CheckInGrid(int x, int y) {
      return !(x >= gridSizeWidth || x < 0 || y < 0 || y >= gridSizeHeight);
   }

   /**
	 * Meme chose que la fonction précédente, mais en plus vérifies avec une taille d'objet
	 */
   public bool CheckBoundaries(int x, int y, int width, int height) {
      if (!CheckInGrid(x, y))
         return false;
      if (!CheckInGrid(x + width - 1, y + height - 1))
         return false;

      return true;
   }

   /**
	 * Nous donne un objet d'overlap lors de la pose d'un item dans la grille
	 * Si on overlap plus d'un objet, alors on renvoie false. Si on overlap un seul objet, on
	 * change (via le mot clef ref) cet objet et on renvoies true. Sinon l'overlap est null et on renvoies true
	 */
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

   /**
	 * Regarde si l'objet la place est prise
	 */
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

   /**
	 * Affiche la grille (débug)
	 */
   public void ViewGrid() {
      String toPrint = "";
      for (int i = 0; i < gridSizeWidth; i++) {
         for (int j = 0; j < gridSizeHeight; j++) {
            if (inventoryItemSlot[i, j] != null)
               toPrint += ("[" + i + "," + j + "] = " + inventoryItemSlot[i, j].itemData.category + ".");
         }
         toPrint += "\n";
      }

      print(toPrint);
   }

   /**
	 * Donnes les coordonées pour placer un objet dans l'inventaire
	 */
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
