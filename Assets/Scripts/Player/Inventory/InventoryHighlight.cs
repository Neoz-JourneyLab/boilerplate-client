using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryHighlight : MonoBehaviour {
   [SerializeField] RectTransform highlighter;

   public void SetSize(InventoryItem item) {
      Vector2 size = new Vector2();
      size.x = item.itemData.width * ItemGrid.tileSizeWidth;
      size.y = item.itemData.height * ItemGrid.tileSizeHeight;

      highlighter.sizeDelta = size;
   }

   public void SetPosition(ItemGrid grid, InventoryItem item) {
      SetParent(grid);

      Vector2 pos = grid.CalculatePositionOnGrid(item, item.onGridPosX, item.onGridPosY);

      highlighter.localPosition = pos;
   }

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
      print(b);
      highlighter.gameObject.SetActive(b);
   }
}
