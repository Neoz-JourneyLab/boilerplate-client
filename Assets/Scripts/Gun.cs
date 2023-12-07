using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Gun : MonoBehaviour {
   [SerializeField] private int dmg;
   [SerializeField] private int ammo;
   [SerializeField] private int maxAmmo;
   [SerializeField] private float fireRate;
   [SerializeField] private bool auto;

   private float nextShot;

   public Gun(int dmg, int ammo, int maxAmmo, float fireRate, bool auto) {
      this.dmg = dmg;
      this.ammo = ammo;
      this.maxAmmo = maxAmmo;
      this.fireRate = fireRate;
      this.auto = auto;


   }

   public int GetDamages() {
      return dmg;
   }
   public int GetAmmo() {
      return ammo;
   }

   public void Refill() {
      this.ammo = this.maxAmmo;
   }

   public bool Shoot() {
      if (ammo <= 0 || Time.time < nextShot) {
         return false;
      }

      ammo--;
      nextShot = Time.time + fireRate;
      return true;
   }
}

