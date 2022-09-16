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

   Gun equipedGun;
   // Start is called before the first frame update
   void Start() {
      ChangeGun(startingGun);
   }

   // Update is called once per frame
   void Update() {
      if (equipedGun == null)
         return;

      batteryBar.localScale = new Vector3(equipedGun.GetComponent<FlashLight>().batteryPercent, 1, 1);
   }

   void ChangeGun(Gun newGun) {
      if (equipedGun != null)
         Destroy(equipedGun);
      equipedGun = Instantiate(newGun, gunHolder);
      equipedGun.name = newGun.name;
      UpdateText();
   }

   public void UpdateText() {
      gunNameText.text = equipedGun.name.ToUpper();
      ammoText.text = equipedGun.capacity + "/" + equipedGun.maxCapacity;
   }

   void OnShoot() {
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
      equipedGun.GetComponent<FlashLight>().ChangeFlashlight();
      GameObject.FindObjectOfType<AudioManager>().PlaySound(lightAudio);
   }

   void OnReload() {
      equipedGun.Reload();
   }

   void OnCheatFL() {
      equipedGun.GetComponent<FlashLight>().RefillLight();
   }
}
