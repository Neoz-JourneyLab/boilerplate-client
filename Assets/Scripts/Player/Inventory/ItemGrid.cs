using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemGrid : MonoBehaviour {
   const float tileSize = 100;

   RectTransform rt;

   Vector2 positionOnGrid = new Vector2();
   Vector2Int tileGridPos = new Vector2Int();

   public void Start() {
      rt = GetComponent<RectTransform>();
   }


   public Vector2Int GetTileGridPosition(Vector2 mousePos) {
      positionOnGrid.x = mousePos.x - rt.position.x;
      positionOnGrid.y = rt.position.y - mousePos.y;

      tileGridPos.x = (int)(positionOnGrid.x / tileSize);
      tileGridPos.y = (int)(positionOnGrid.y / tileSize);

      return tileGridPos;
   }

}
