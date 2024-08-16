using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AuctionHouse : MonoBehaviour {
  [Header("Prefabs")]
  [SerializeField] GameObject inventory_row_prefab;
  [SerializeField] GameObject our_items_prefab;
  [SerializeField] GameObject other_items_prefab;

  [Header("Scroll Instances")]
  [SerializeField] GameObject inventory_grid_scroll;
  [SerializeField] GameObject our_items_scroll;
  [SerializeField] GameObject other_items_scroll;

  [Header("Input fields")]
  [SerializeField] TMP_InputField input_filter_inventory;
  [SerializeField] TMP_InputField input_filter_sell;

  [Header("Private")]
  List<Item> inventory = new List<Item>();
  List<ItemOther> other_items = new List<ItemOther>();
  Dictionary<string, ItemDetail> items_bdd = new Dictionary<string, ItemDetail>() {
    {"blé", new ItemDetail(){ consumable = false, category = "cereal" } },
    {"orge", new ItemDetail(){ consumable = false, category = "cereal" } },
    {"malt", new ItemDetail(){ consumable = false, category = "cereal" } },
    {"chanvre", new ItemDetail(){ consumable = false, category = "cereal" } },
    {"riz", new ItemDetail(){ consumable = false, category = "cereal" } },
    {"avoine", new ItemDetail(){ consumable = false, category = "cereal" } },

    {"potion de soin", new ItemDetail(){ consumable = true, category = "potion" } },
    {"potion de chasseur de trésor", new ItemDetail(){ consumable = true, category = "potion" } },
    {"potion de kolizéum", new ItemDetail(){ consumable = true, category = "potion" } },
    {"potion spirituelle de martalo", new ItemDetail(){ consumable = true, category = "potion" } },
    {"potion turbulente", new ItemDetail(){ consumable = true, category = "potion" } },

    {"aile de boulard", new ItemDetail(){ consumable = false, category = "wings", level = 15 } },
    {"aile de scarafeuille rouge", new ItemDetail(){ consumable = false, category = "wings", level = 65 }},
    {"aile de scarafeuille vert", new ItemDetail(){ consumable = false, category = "wings", level = 33 }},
    {"aile de tofu maléfique", new ItemDetail(){ consumable = false, category = "wings", level = 17 }},
    {"ailes cassées", new ItemDetail(){ consumable = false, category = "wings", level = 29 } } ,
  };
  List<string> itemsList = new List<string>();

  void Start() {
    foreach (var item in Resources.LoadAll("sprites/items", typeof(Sprite))) {
      itemsList.Add(item.name);
    }
    CreateRandomInventory();
    CreateRandomAuctionHouse();
  }

  void CreateRandomInventory() {
    for (int i = 0; i < 200; i++) {
      inventory.Add(new Item() {
        label = itemsList[Random.Range(0, 256) % itemsList.Count],
        amount = Random.Range(1, 999)
      });
    }
  }

  void CreateRandomAuctionHouse() {
    for (int i = 0; i < 10; i++) {
      foreach (var item in itemsList) {
        long price = Random.Range(1, 999);
        other_items.Add(new ItemOther() {
          amount = 1,
          label = item,
          price_1 = price,
          price_10 = (int)(price * Random.Range(8f, 12f)),
          price_100 = (int)(price * Random.Range(80f, 120f)),
        });
      }
    }
    ItemOther[] tmp = new ItemOther[other_items.Count];
    for (int i = 0; i < other_items.Count; i++) {
      ItemOther item = other_items[i];
      tmp[i] = item;
      tmp[i].label = Random.Range(0, 9999).ToString("0000");
    }
    string json = JsonConvert.SerializeObject(other_items, Formatting.Indented);
    File.WriteAllText("Assets/Resources/auction_house.json", json);
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

  string category_filter = "";
  public void FilterAuctionHouse(string _category_filter) {
    if (category_filter == _category_filter) {
      category_filter = "";
    } else {
      category_filter = _category_filter;
    }
    DrawAuctionHouseOtherItems();
  }

  public void DrawAuctionHouseOtherItems() {
    foreach (Transform children in other_items_scroll.transform) {
      Destroy(children.gameObject);
    }
    string filter = input_filter_sell.text;
    if (filter.Length < 3 && category_filter == "") return;
    DateTime s = DateTime.UtcNow;

    foreach (var item in other_items) {
      string category = items_bdd[item.label].category;
      if (category_filter != "" && category_filter != category) continue;
      if (filter != "" && !item.label.Contains(filter)) continue;

      GameObject current_row = Instantiate(other_items_prefab, other_items_scroll.transform);
      Transform main_row = current_row.transform.Find("main_row");
      main_row.Find("icon").GetComponent<Image>().sprite = SpriteManager.GetSprite("items/" + item.label);
      main_row.Find("label").GetComponent<TMP_Text>().text = item.label;
      main_row.Find("category").GetComponent<TMP_Text>().text = category;
      main_row.Find("level").GetComponent<TMP_Text>().text = items_bdd[item.label].level.ToString();
      main_row.Find("price").GetComponent<TMP_Text>().text = ((int)(item.price_100 / 100) + (item.price_10 / 10) + (item.price_1 / 1)) + "k";

      foreach (string str in new string[3] { "x1", "x10", "x100" }) {
        Transform row_price = current_row.transform.Find(str);
        row_price.Find("icon").GetComponent<Image>().sprite = SpriteManager.GetSprite("items/" + item.label);
        row_price.Find("label").GetComponent<TMP_Text>().text = item.label;
        row_price.Find("price").GetComponent<TMP_Text>().text = (str == "x1" ? item.price_1 : (str == "x10" ? item.price_10 : item.price_100)) + "k";
      }
    }
    print("draw auction house in " + (DateTime.UtcNow - s).TotalMilliseconds + " ms");
  }

  bool select_all = false;
  public void SelectAllSell() {
    select_all = !select_all;
    foreach (Transform item in our_items_scroll.transform) {
      item.GetComponentInChildren<CheckBox>().Set(select_all);
    }
  }
}

public class Item {
  public string label;
  public string id = Guid.NewGuid().ToString().Substring(0, 6);
  public long amount;
}

public class ItemOther : Item {
  public long price_1;
  public long price_10;
  public long price_100;
}

public class ItemDetail {
  public bool consumable = false;
  public string category;
  public short level = 1;
}