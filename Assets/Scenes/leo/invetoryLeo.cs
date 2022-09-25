using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class invetoryLeo : MonoBehaviour
{
  [SerializeField] GameObject gridPrefab;
  [SerializeField] GameObject gridLinePrefab;
  [SerializeField] int lines;
  [SerializeField] int columns;

  List<InventoryItem> items = new List<InventoryItem>() {
    new InventoryItem(){ quantity = 1, id = "1234",
      model = new ItemModel(){ alias = "pamas", id = "pamasID", category = "weapon", equipable = true, height = 2, width = 2, maxStack = 1
      }, onGridPosX = 2, onGridPosY = 2, rotated = false }
  };

  private void Start() {
    foreach (var item in items) {
      item.model.SetIcon();
    }

    print(GetComponent<RectTransform>().rect.height + "/" + gridLinePrefab.GetComponent<RectTransform>().rect.height);
    lines = Mathf.FloorToInt(GetComponent<RectTransform>().rect.height / gridLinePrefab.GetComponent<RectTransform>().rect.height);
		columns = Mathf.FloorToInt(GetComponent<RectTransform>().rect.width / gridLinePrefab.GetComponent<RectTransform>().rect.height);
    for (int x = 0; x < lines; x++) {
			GameObject gridLine = Instantiate(gridLinePrefab, transform);
      gridLine.name = "line_" + x;
			for (int y = 0; y < columns; y++) {
				GameObject gridCase = Instantiate(gridPrefab, gridLine.transform);
        gridCase.name = "case_" + x + "_" + y;
        gridCase.GetComponent<casePrefab>().x = x;
        gridCase.GetComponent<casePrefab>().y = y;
			}
    }

    foreach (var item in items) {
      for (int x = 0; x < item.model.width; x++) {
        for (int y = 0; y < item.model.height; y++) {
          GameObject.Find("case_" + (x + item.onGridPosX) + "_" + (y + item.onGridPosY)).GetComponent<Image>().color = new Color(0, 0, 0, 1);
          GameObject.Find("case_" + (x + item.onGridPosX) + "_" + (y + item.onGridPosY)).GetComponent<casePrefab>().takenBy = item.id;
				}
      }

      var topCase = GameObject.Find("case_" + (item.onGridPosX) + "_" + (item.onGridPosY));

			GameObject sprite = Instantiate(gridPrefab, transform.parent);
      sprite.GetComponent<Image>().sprite = item.model.icon;
      sprite.transform.position = topCase.transform.localPosition;
      sprite.transform.localScale = new Vector2(item.model.width, item.model.height);
		}
  }
}
