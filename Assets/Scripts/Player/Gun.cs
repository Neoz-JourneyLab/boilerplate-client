using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Gun : MonoBehaviour {
   public float fireRate;
   public float capacity;
   public float maxCapacity;
   public float reloadTime;
   public int damages;

   public bool automatic;
   public AudioSource shootAudio;
   public AudioSource clickAudio;
   public AudioSource reloadAudio;
   public Transform muzzle;

   public GameObject particles;
   public Light muzzleFlash;

   bool canShoot;

   float nextShot;
   // Start is called before the first frame update
   void Start() {
      nextShot = Time.time;
      capacity = maxCapacity;
      canShoot = true;
   }

   // Update is called once per frame
   void Update() {
   }

   public Transform Shoot() {
      if (!canShoot)
         return null;
      if (capacity <= 0) {
         GameObject.FindObjectOfType<AudioManager>().PlaySound(clickAudio);
         return null;
      }
      if (nextShot > Time.time)
         return null;

      nextShot = Time.time + fireRate;
      capacity--;

      ShotAnim();
      GameObject.FindObjectOfType<AudioManager>().PlaySound(shootAudio);
      uWebSocketManager.EmitEv("shot");

      Ray ray = new Ray(muzzle.transform.position, muzzle.transform.forward);
      RaycastHit hit;

      Debug.DrawRay(ray.origin, ray.direction, Color.red, 0.5f);

      if (!Physics.Raycast(ray, out hit))
         return null;

      return hit.collider.transform;
   }

   public void ShotAnim() {
      muzzleFlash.gameObject.SetActive(true);
      Invoke(nameof(DisableLight), 0.05f);
      GetComponent<Animator>().Play("shoot");
      GameObject ps = Instantiate(particles, muzzle.transform.position, muzzle.transform.rotation);
      ps.GetComponent<ParticleSystem>().Play();
      Destroy(ps, ps.GetComponent<ParticleSystem>().main.duration + 0.1f);
   }

   void DisableLight() {
      muzzleFlash.gameObject.SetActive(false);
   }

   public void Reload() {
      canShoot = false;
      Invoke(nameof(ReloadEnd), reloadTime);
      GameObject.FindObjectOfType<AudioManager>().PlaySound(reloadAudio);
      GetComponent<Animator>().Play("reload");
   }

   void ReloadEnd() {
      canShoot = true;
      if (capacity == 0)
         capacity = maxCapacity;
      else
         capacity = maxCapacity + 1;
      GetComponent<Animator>().SetBool("reload", false);
      transform.parent.parent.GetComponent<GunControl>().UpdateText();
   }
}
