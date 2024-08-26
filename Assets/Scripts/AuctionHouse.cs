using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AuctionHouse : MonoBehaviour {
  [Header("Prefabs")]
  [SerializeField] GameObject inventory_row_prefab;
  [SerializeField] GameObject our_items_prefab;
  [SerializeField] GameObject other_items_prefab;
  [SerializeField] GameObject other_items_than_we_sell_prefab;

  [Header("Scroll Instances")]
  [SerializeField] GameObject inventory_grid_scroll;
  [SerializeField] GameObject our_items_scroll;
  [SerializeField] GameObject other_items_scroll;
  [SerializeField] GameObject other_items_than_we_sell_scroll;

  [Header("Pools")]
  [SerializeField] List<GameObject> pool = new List<GameObject>();
  private int totalItems;
  private int visibleItemCount;
  private float itemHeight;
  private float scrollHeight;

  [Header("Input fields")]
  [SerializeField] TMP_InputField input_filter_inventory;
  [SerializeField] TMP_InputField input_filter_sell;

  [Header("ModifiySell")]
  [SerializeField] Image modify_sell_icon;
  [SerializeField] TMP_Text modify_sell_label;
  [SerializeField] TMP_InputField modify_sell_actual_price;
  [SerializeField] TMP_Text modify_sell_quantity;
  [SerializeField] TMP_Text modify_sell_time;
  [SerializeField] TMP_Text modify_sell_avg_price;


  [Header("Private")]
  List<Item> inventory = new List<Item>();
  List<ItemOther> other_items = new List<ItemOther>();
  List<OurItems> our_items = new List<OurItems>();
  Dictionary<string, ItemDetail> items_bdd = new Dictionary<string, ItemDetail>();
  ItemSoldFromServer[] items_sold_from_server = new ItemSoldFromServer[0];
  List<string> itemsList = new List<string>();

  void Start() {
    foreach (var item in Resources.LoadAll("sprites/items", typeof(Sprite))) {
      itemsList.Add(item.name);
      items_bdd.Add(item.name, new ItemDetail() { category = item.name.Split("_")[0], consumable = item.name.StartsWith("potion"), level = (short)Random.Range(1, 200) });
    }
    CreateRandomInventory();
    CreateRandomOurItemsForSale();

  }

  IEnumerator InitializeAfterLayout() {
    yield return new WaitForEndOfFrame(); // Wait until the layout has been calculated
    InitializePool();
    other_items_scroll.GetComponentInParent<ScrollRect>().onValueChanged.AddListener(OnScrollChanged);
    UpdateVisibleItems();
  }

  public void StartHttp() {
    StartCoroutine(GetItemInDb());
  }

  IEnumerator GetItemInDb() {
    DateTime start_req = DateTime.UtcNow;

    UnityWebRequest request = UnityWebRequest.Get("https://ac13-46-193-68-103.ngrok-free.app" + "/get_items");
    JSONObject json = new JSONObject();
    json.AddField("filter", "");
    string jsonRequestBody = json.ToString();
    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestBody);
    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
    request.downloadHandler = new DownloadHandlerBuffer();
    request.SetRequestHeader("Content-Type", "application/json");
    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success) {
      print("Fin de la requete http : " + (int)(DateTime.UtcNow - start_req).TotalMilliseconds + " ms");
      items_sold_from_server = JsonConvert.DeserializeObject<ItemSoldFromServer[]>(request.downloadHandler.text);
      print("Objets convertis en JSON, prets a être affichés : " + (int)(DateTime.UtcNow - start_req).TotalMilliseconds + " ms");
      print(request.downloadHandler.text.Length);
      print(request.downloadHandler.text.Substring(0, 1000));
      totalItems = items_sold_from_server.Length;
    } else {
      print("ERROR !" + request.downloadHandler.text + " > " + request.error);
    }

    request.Dispose();

    StartCoroutine(InitializeAfterLayout());
  }


  // Create enough pooled items to fill the viewport
  void InitializePool() {
    RectTransform parentRectTransform = other_items_scroll.transform.parent.parent.GetComponent<RectTransform>();
    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRectTransform);

    itemHeight = other_items_prefab.GetComponent<RectTransform>().rect.height;
    scrollHeight = parentRectTransform.rect.height; // Now, this should return the correct height
    Debug.Log("item h " + itemHeight + " & scroll h " + scrollHeight + " > " + other_items_scroll.transform.parent.parent.name);
    visibleItemCount = Mathf.CeilToInt(scrollHeight / itemHeight) + 1;

    for (int i = 0; i < visibleItemCount; i++) {
      GameObject item = Instantiate(other_items_prefab, other_items_scroll.transform);
      pool.Add(item);
    }
    other_items_scroll.GetComponent<RectTransform>().sizeDelta = new Vector2(other_items_scroll.GetComponent<RectTransform>().sizeDelta.x, totalItems * itemHeight);
  }

  // Called whenever the ScrollRect's value changes (i.e., the user scrolls)
  void OnScrollChanged(Vector2 scrollPosition) {
    UpdateVisibleItems();
  }

  // Update the visible items in the pool
  int last_first = -1;
  void UpdateVisibleItems() {
    float contentPosY = other_items_scroll.GetComponent<RectTransform>().anchoredPosition.y;
    int firstVisibleIndex = Mathf.FloorToInt(contentPosY / itemHeight);
    if (last_first == firstVisibleIndex) return;
    last_first = firstVisibleIndex;
    firstVisibleIndex = Mathf.Clamp(firstVisibleIndex, 0, totalItems - visibleItemCount);

    for (int i = 0; i < visibleItemCount; i++) {
      int dataIndex = firstVisibleIndex + i;
      if (dataIndex < totalItems) {
        UpdateItem(pool[i], dataIndex);
      }
    }
  }

  // Update the content of a pooled item
  void UpdateItem(GameObject item, int dataIndex) {
    var dataItem = items_sold_from_server[dataIndex];

    item.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -dataIndex * itemHeight);
    Transform main_row = item.transform.Find("main_row");
    main_row.Find("icon").GetComponent<Image>().sprite = SpriteManager.GetSprite("items/" + dataItem.label);
    main_row.Find("label").GetComponent<TMP_Text>().text = dataItem.label;
    main_row.Find("price").GetComponent<TMP_Text>().text = dataItem.price + "k";
  }

  void CreateRandomOurItemsForSale() {
    for (int i = 0; i < 200; i++) {
      our_items.Add(new OurItems() {
        label = itemsList[Random.Range(0, 256) % itemsList.Count],
        price = Random.Range(1, 999),
        amount = Random.Range(0, 256) % 2 == 0 ? 1 : (Random.Range(0, 256) % 2 == 0 ? 10 : 100),
        expire_at = DateTime.UtcNow.AddDays(Random.Range(5, 25))
      });
    }
  }

  void CreateRandomInventory() {
    for (int i = 0; i < 2000; i++) {
      inventory.Add(new Item() {
        label = itemsList[Random.Range(0, 256) % itemsList.Count],
        amount = Random.Range(1, 999)
      });
    }
  }

  void CreateRandomAuctionHouse() {
    foreach (var item in itemsList) {
      long price = Random.Range(1, 999);
      other_items.Add(new ItemOther() {
        label = item,
        price_1 = price,
        price_10 = (int)(price * Random.Range(8f, 12f)),
        price_100 = (int)(price * Random.Range(80f, 120f)),
      });
    }
    // Initialize the data (in real case, this would come from your database or other source)
    totalItems = other_items.Count;
    /*
    ItemOther[] tmp = new ItemOther[other_items.Count];
    for (int i = 0; i < other_items.Count; i++) {
      ItemOther item = other_items[i];
      tmp[i] = item;
      tmp[i].label = Random.Range(0, 9999).ToString("0000");
    }
    string json = JsonConvert.SerializeObject(other_items, Formatting.Indented);
    File.WriteAllText("Assets/Resources/auction_house.json", json);*/
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

  public void DrawAuctionHouseOurSells() {
    foreach (Transform children in our_items_scroll.transform) {
      Destroy(children.gameObject);
    }
    DateTime s = DateTime.UtcNow;

    foreach (var item in our_items) {
      GameObject current_row = Instantiate(our_items_prefab, our_items_scroll.transform);
      Transform main_row = current_row.transform;
      main_row.Find("icon").GetComponent<Image>().sprite = SpriteManager.GetSprite("items/" + item.label);
      main_row.Find("label").GetComponent<TMP_Text>().text = item.label;
      main_row.Find("time").GetComponent<TMP_Text>().text = ((int)(item.expire_at - DateTime.UtcNow).TotalDays) + "j";
      main_row.Find("qtt").GetComponent<TMP_Text>().text = item.amount.ToString();
      main_row.Find("price").GetComponent<TMP_Text>().text = (item.price) + "k";

      main_row.GetComponent<Button>().onClick.AddListener(() => {
        modify_sell_avg_price.text = "Prix moyen : " + (int)(item.price * Random.Range(0.5f, 1.5f)) + "k";
        modify_sell_actual_price.text = item.price.ToString();
        modify_sell_icon.sprite = SpriteManager.GetSprite("items/" + item.label);
        modify_sell_label.text = item.label;
        modify_sell_quantity.text = "x" + item.amount;
        modify_sell_time.text = "Temps restant : " + ((int)(item.expire_at - DateTime.UtcNow).TotalDays) + "j";

        foreach (Transform children in other_items_than_we_sell_scroll.transform) {
          Destroy(children.gameObject);
        }
        for (int i = 0; i < 3; i++) {
          GameObject other_than_well_sell = Instantiate(other_items_than_we_sell_prefab, other_items_than_we_sell_scroll.transform);
          other_than_well_sell.transform.Find("icon").GetComponent<Image>().sprite = SpriteManager.GetSprite("items/" + item.label);
          other_than_well_sell.transform.Find("qtt").GetComponent<TMP_Text>().text = "x" + (i == 0 ? 1 : (i == 1 ? 10 : 100));
          other_than_well_sell.transform.Find("price").GetComponent<TMP_Text>().text =
            ((int)(item.price * (i == 0 ? 1 : (i == 1 ? 10 : 100)) * Random.Range(0.5f, 0.95f))) + "k";
        }
      });
    }
    print("draw our items in " + (DateTime.UtcNow - s).TotalMilliseconds + " ms");
  }

  public void DrawAuctionHouseOtherItems() {
    foreach (Transform children in other_items_scroll.transform) {
      Destroy(children.gameObject);
    }
    string filter = input_filter_sell.text;
    if (filter.Length < 3 && category_filter == "") return;

    DateTime s = DateTime.UtcNow;

    var filtred = filter == "" ? other_items : other_items.Where(x => x.label.Contains(filter));
    other_items_scroll.gameObject.SetActive(false);
    int l = 0;

    foreach (var item in filtred) {
      DateTime s1 = DateTime.UtcNow;
      string category = items_bdd[item.label].category;
      GameObject current_row = Instantiate(other_items_prefab, other_items_scroll.transform);
      Transform main_row = current_row.transform.Find("main_row");
      main_row.Find("icon").GetComponent<Image>().sprite = SpriteManager.GetSprite("items/" + item.label);

      main_row.Find("label").GetComponent<TMP_Text>().text = item.label;
      main_row.Find("category").GetComponent<TMP_Text>().text = category;
      main_row.Find("level").GetComponent<TMP_Text>().text = items_bdd[item.label].level.ToString();
      main_row.Find("price").GetComponent<TMP_Text>().text = ((int)(item.price_100 / 100) + (item.price_10 / 10) + (item.price_1 / 1)) + "k";

      main_row.GetComponent<Button>().onClick.AddListener(() => {
        foreach (string str in new string[3] { "x1", "x10", "x100" }) {
          Transform row_price = current_row.transform.Find(str);
          row_price.Find("icon").GetComponent<Image>().sprite = SpriteManager.GetSprite("items/" + item.label);
          row_price.Find("label").GetComponent<TMP_Text>().text = item.label;
          row_price.Find("price").GetComponent<TMP_Text>().text = (str == "x1" ? item.price_1 : (str == "x10" ? item.price_10 : item.price_100)) + "k";
        }
      });
    }

    other_items_scroll.gameObject.SetActive(true);
    LayoutRebuilder.ForceRebuildLayoutImmediate(other_items_scroll.GetComponent<RectTransform>());
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

public class OurItems : Item {
  public long price;
  public DateTime expire_at;
}

public class ItemDetail {
  public bool consumable = false;
  public string category;
  public short level = 1;
}

public class ItemSoldFromServer {
  public int price;
  public short quantity;
  public string label;
}