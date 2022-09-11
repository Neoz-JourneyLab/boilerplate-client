using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour {
   public float fireRate;
   public float capacity;
   public float maxCapacity;
   public float reloadTime;
   public int damages;

   public bool automatic;

   public Transform muzzle;

   float nextShot;
   // Start is called before the first frame update
   void Start() {
      nextShot = Time.time;
      capacity = maxCapacity;
   }

   // Update is called once per frame
   void Update() {

   }

   public Transform Shoot() {
      print(1);
      if (capacity <= 0)
         return null;
      print(2);
      if (nextShot > Time.time)
         return null;
      print(3);

      nextShot = Time.time + fireRate;
      capacity--;

      Ray ray = new Ray(muzzle.transform.position, muzzle.transform.forward);
      RaycastHit hit;

      Debug.DrawRay(ray.origin, ray.direction, Color.red, 0.5f);

      if (!Physics.Raycast(ray, out hit))
         return null;
      print(4);

      return hit.collider.transform;
   }

   public void Reload() {
      Invoke(nameof(ReloadEnd), reloadTime);
   }

   void ReloadEnd() {
      if (capacity == 0)
         capacity = maxCapacity;
      else
         capacity = maxCapacity + 1;
   }
}
