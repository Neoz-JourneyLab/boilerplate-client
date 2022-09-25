using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GunControl : MonoBehaviour {
   public Gun startingGun;
   public Transform gunHolder;

   public TextMeshProUGUI gunNameText;
   public TextMeshProUGUI ammoText;
   public Transform batteryBar;

   public AudioSource lightAudio;
   public bool canShoot;
   public bool shooting;

   internal Gun equipedGun;
   // Start is called before the first frame update
   void Start() {
      if (startingGun != null)
         EquipGun(startingGun);
      else
         UnequipGun();
   }

   // Update is called once per frame
   void Update() {
      if (equipedGun == null)
         return;

      batteryBar.localScale = new Vector3(equipedGun.GetComponent<FlashLight>().batteryPercent, 1, 1);
      if (shooting)
         Shoot();
   }

   public void EquipGun(Gun newGun) {
      if (equipedGun != null)
         Destroy(equipedGun);
      equipedGun = Instantiate(newGun, gunHolder);
      equipedGun.name = newGun.name;
      GameObject.FindGameObjectWithTag("Player").GetComponent<PositionSender>().flash =
          equipedGun.GetComponent<FlashLight>().flashlight.GetComponent<Light>() ?? null;
      UpdateText();
      canShoot = true;
   }

   public void EquipGun(Gun newGun, int ammo) {
      if (equipedGun != null)
         Destroy(equipedGun);
      equipedGun = Instantiate(newGun, gunHolder);
      equipedGun.name = newGun.name;
      equipedGun.capacity = ammo;
      GameObject.FindGameObjectWithTag("Player").GetComponent<PositionSender>().flash =
          equipedGun.GetComponent<FlashLight>().flashlight.GetComponent<Light>() ?? null;
      UpdateText();
      canShoot = true;
   }

   public void UnequipGun() {
      if (equipedGun != null) {
         Destroy(equipedGun.gameObject);
         equipedGun = null;
      }
      UpdateText();
      canShoot = false;
   }

   public int UnequipGunAmmo() {
      int toReturn = equipedGun.capacity;
      Destroy(equipedGun.gameObject);
      equipedGun = null;

      UpdateText();
      canShoot = false;
      return toReturn;
   }

   public void UpdateText() {
      if (equipedGun != null) {
         gunNameText.text = equipedGun.name.ToUpper();
         ammoText.text = equipedGun.capacity + "/" + equipedGun.maxCapacity;
      } else {
         gunNameText.text = "";
         ammoText.text = "";
      }
   }

   void Shoot() {
      if (equipedGun == null)
         return;
      if (!canShoot)
         return;
      Transform hit = equipedGun.Shoot();
      ammoText.text = equipedGun.capacity + "/" + equipedGun.maxCapacity;

      if (!equipedGun.automatic)
         shooting = false;

      //Notify server we shot 
      if (hit == null)
         return;
      if (hit.GetComponent<Zombie>() == null)
         return;

      //Notify server we hit a zombie
      uWebSocketManager.EmitEv("hit:zombie", new { zid = hit.GetComponent<Zombie>().id, equipedGun.damages });
     
   }

   void OnShoot() {
      shooting = true;
   }

   void OnFlashlight() {
      if (equipedGun == null)
         return;
      equipedGun.GetComponent<FlashLight>().ChangeFlashlight();
      GameObject.FindObjectOfType<AudioManager>().PlaySound(lightAudio);
   }

   void OnReload() {
      int ammoToReload = equipedGun.maxCapacity - equipedGun.capacity;

      if (equipedGun == null)
         return;
      if (equipedGun.capacity == equipedGun.maxCapacity)
         return;

      foreach (var item in FindObjectOfType<InventoryController>().playerItems.Where(i => i.model.alias == equipedGun.ammoType.ToString())) {
         if (ammoToReload < item.quantity) {
            item.quantity -= ammoToReload;
            ammoToReload = 0;
            break;
         } else {
            ammoToReload -= item.quantity;
            item.quantity = 0;
         }
         uWebSocketManager.EmitEv("update:stack", new { item.id, item.quantity });
      }
      FindObjectOfType<InventoryController>().playerItems =
         FindObjectOfType<InventoryController>().playerItems.Where(i => i.quantity > 0).ToList();


      equipedGun.Reload(equipedGun.maxCapacity - ammoToReload);
   }

   void OnStopShoot() {
      shooting = false;
   }

   void OnCheatFL() {
      if (equipedGun == null) return;
      var battery = FindObjectOfType<InventoryController>().playerItems.FirstOrDefault(i => i.model.alias == ItemAlias.battery.ToString());
      if (battery != null) {
         battery.quantity--;
         if (battery.quantity <= 0) {
            FindObjectOfType<InventoryController>().playerItems.Remove(battery);
         }
         equipedGun.GetComponent<FlashLight>().RefillLight();
      } else {
         print("no flash battery !");
      }
   }
}
