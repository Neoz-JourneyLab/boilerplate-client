using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GunControl : MonoBehaviour {
   public Gun startingGun;
   public Transform gunHolder;

   public TextMeshProUGUI gunNameText;
   public TextMeshProUGUI ammoText;
   public Transform batteryBar;

   public AudioSource lightAudio;

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
   }

   public void EquipGun(Gun newGun) {
      if (equipedGun != null)
         Destroy(equipedGun);
      equipedGun = Instantiate(newGun, gunHolder);
      equipedGun.name = newGun.name;
      UpdateText();
   }

   public void EquipGun(Gun newGun, int ammo) {
      if (equipedGun != null) Destroy(equipedGun);
      equipedGun = Instantiate(newGun, gunHolder);
      equipedGun.name = newGun.name;
      equipedGun.capacity = ammo;
      GameObject.FindGameObjectWithTag("Player").GetComponent<PositionSender>().flash =
         equipedGun.GetComponent<FlashLight>().flashlight.GetComponent<Light>() ?? null;
      UpdateText();
   }

   public void UnequipGun() {
      if (equipedGun != null) {
         Destroy(equipedGun.gameObject);
         equipedGun = null;
      }
      UpdateText();
   }

   public int UnequipGunAmmo() {
      int toReturn = equipedGun.capacity;
      Destroy(equipedGun.gameObject);
      equipedGun = null;

      UpdateText();
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

   void OnShoot() {
      if (equipedGun == null)
         return;
      Transform hit = equipedGun.Shoot();
      ammoText.text = equipedGun.capacity + "/" + equipedGun.maxCapacity;
      //Notify server we shot 
      if (hit == null)
         return;
      if (hit.GetComponent<Zombie>() == null)
         return;

      //Notify server we hit a zombie
      uWebSocketManager.EmitEv("hit:zombie", new { zid = hit.GetComponent<Zombie>().id, equipedGun.damages });
   }

   void OnFlashlight() {
      if (equipedGun == null)
         return;
      equipedGun.GetComponent<FlashLight>().ChangeFlashlight();
      GameObject.FindObjectOfType<AudioManager>().PlaySound(lightAudio);
   }

   void OnReload() {
      if (equipedGun != null)
         equipedGun.Reload();
   }

   void OnCheatFL() {
      if (equipedGun != null)
         equipedGun.GetComponent<FlashLight>().RefillLight();
   }
}
