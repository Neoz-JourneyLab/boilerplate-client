using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunControl : MonoBehaviour {
   Gun equipedGun;
   // Start is called before the first frame update
   void Start() {
      equipedGun = transform.GetComponentInChildren<Gun>();
   }

   // Update is called once per frame
   void Update() {

   }

   void OnShoot() {
      Transform hit = equipedGun.Shoot();
      //Notify server we shot 
      if (hit == null)
         return;
      if (hit.GetComponent<Zombie>() == null)
         return;

      //Notify server we hit a zombie
      uWebSocketManager.EmitEv("hitzombie", new { id = transform.GetComponent<Zombie>().id });
   }

   void OnReload() {
      equipedGun.Reload();
   }
}
