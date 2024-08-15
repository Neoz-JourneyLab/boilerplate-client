using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AuctionHouse : MonoBehaviour {
  [Header("Prefabs")]
  [SerializeField] GameObject inventory_row_prefab;
  [SerializeField] GameObject sell_prefab;
  [SerializeField] GameObject other_items_prefab;

  [Header("Scroll Instances")]
  [SerializeField] GameObject inventory_grid_scroll;
  [SerializeField] GameObject sells_scroll;
  [SerializeField] GameObject other_items_scroll;

  [Header("Input fields")]
  [SerializeField] TMP_InputField input_filter_inventory;

  [Header("Private")]
  List<Item> inventory = new List<Item>();
  Dictionary<string, ItemDetail> items_bdd = new Dictionary<string, ItemDetail>() {
    {"blé", new ItemDetail(){ consumable = false } },
    {"orge", new ItemDetail(){ consumable = false } },
    {"malt", new ItemDetail(){ consumable = false } },
    {"chanvre", new ItemDetail(){ consumable = false } },
    {"riz", new ItemDetail(){ consumable = false } },
    {"avoine", new ItemDetail(){ consumable = false } },
    {"potion de soin", new ItemDetail(){ consumable = true }
    }
  };

  void Start() {
    DateTime s = DateTime.UtcNow;
    string[] items = new string[7] { "blé", "orge", "malt", "riz", "chanvre", "avoine", "potion de soin" };
    for (int i = 0; i < 1000; i++) {
      inventory.Add(new Item() {
        label = items[Random.Range(0, 256) % 7],
        amount = Random.Range(1, 999)
      });
    }
    print("populate fake inventory in " + (DateTime.UtcNow - s).TotalMilliseconds + " ms");

    DrawInvenotry();
  }

  public void SwapFilterPossible() {
    filter_only_sellable = !filter_only_sellable;
    DrawInvenotry();
  }

  readonly int item_per_row = 5;
  bool filter_only_sellable = false;
  public void DrawInvenotry() {
    DateTime s = DateTime.UtcNow;

    string filter = input_filter_inventory.text;
    if (filter.Length == 1) return;

    int curr = 0;
    GameObject current_row = null;

    foreach (Transform children in inventory_grid_scroll.transform) {
      Destroy(children.gameObject);
    }

    foreach (var item in inventory) {
      if (filter != "" && !item.label.Contains(filter)) continue;
      if (filter_only_sellable && items_bdd[item.label].consumable) continue;

      if (curr % item_per_row == 0) {
        current_row = Instantiate(inventory_row_prefab, inventory_grid_scroll.transform);
      }
      Transform cell = current_row.transform.GetChild(curr % item_per_row);
      cell.GetComponent<Image>().color = new Color(0.184f, 0.19f, 0.28f);
      cell.GetChild(0).gameObject.SetActive(true);
      cell.GetChild(0).GetComponent<Image>().sprite = SpriteManager.GetSprite("items/" + item.label);
      cell.GetChild(0).GetComponentInChildren<TMP_Text>().text = item.amount.ToString();
      curr++;
    }
    print("draw inventory in " + (DateTime.UtcNow - s).TotalMilliseconds + " ms");
  }

  bool select_all = false;
  public void SelectAllSell() {
    select_all = !select_all;
    foreach (Transform item in sells_scroll.transform) {
      item.GetComponentInChildren<CheckBox>().Set(select_all);
    }
  }
}

public class Item {
  public string label;
  public string id;
  public long amount;
}

public class ItemDetail {
  public bool consumable;
}