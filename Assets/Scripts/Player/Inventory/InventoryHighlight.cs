using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Classe servant a surligner les objets quand le curseur passes dessus, ou surligner la case que prendra l'objet
 */
public class InventoryHighlight : MonoBehaviour {
   [SerializeField] RectTransform highlighter;

   public void SetSize(InventoryItem item) {
      Vector2 size = new Vector2();
      size.x = item.WIDTH * ItemGrid.tileSizeWidth;
      size.y = item.HEIGHT * ItemGrid.tileSizeHeight;

      highlighter.sizeDelta = size;
   }

   /**
    * se met sur un objet pour le surligner (sur la grille)
    */
   public void SetPosition(ItemGrid grid, InventoryItem item) {
      SetParent(grid);

      Vector2 pos = grid.CalculatePositionOnGrid(item, item.onGridPosX, item.onGridPosY);

      highlighter.localPosition = pos;
   }

   /**
    * se met sur un la grille et surligne la place que prendra l'objet à la pose
    */
   public void SetPosition(ItemGrid grid, InventoryItem item, int x, int y) {
      SetParent(grid);

      Vector2 pos = grid.CalculatePositionOnGrid(item, x, y);

      highlighter.localPosition = pos;
   }

   private void SetParent(ItemGrid grid) {
      highlighter.SetParent(grid.GetComponent<RectTransform>());
      highlighter.SetAsLastSibling();
   }

   public void Show(bool b) {
      highlighter.gameObject.SetActive(b);
   }
}
