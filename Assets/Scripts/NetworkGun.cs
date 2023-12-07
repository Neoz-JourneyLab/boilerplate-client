using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetworkGun : NetworkBehaviour {
   [SerializeField] private Gun currentGun;
   [SerializeField] private Light muzzleFlash;
   [SerializeField] private Transform bulletHole;
   [SerializeField] private Camera cam;

   [SerializeField] private TextMeshProUGUI ammoText;
   void Start() {
      this.ammoText = GameObject.Find("AmmoText").GetComponent<TextMeshProUGUI>();
      this.ammoText.text = (currentGun.GetAmmo().ToString());
   }

   // Update is called once per frame
   void Update() {
      if (!IsOwner) return;

      if (Input.GetKey(KeyCode.Mouse0)) {
         if (!currentGun.Shoot())
            return;

         Ray ray = new Ray(cam.transform.position, cam.transform.forward);
         Vector3 origin = ray.origin;
         Vector3 direction = ray.direction;

         this.ammoText.text = (currentGun.GetAmmo().ToString());
         ShootServerRpc(OwnerClientId, origin, direction);
      }

      if (Input.GetKeyDown(KeyCode.R)) {
         currentGun.Refill();
         this.ammoText.text = (currentGun.GetAmmo().ToString());
      }
   }

   [ServerRpc(RequireOwnership = false)]
   private void ShootServerRpc(ulong clientID, Vector3 origin, Vector3 direction) {
      Transform client = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientID).gameObject.transform;
      Camera cam = client.GetComponentInChildren<Camera>(true);

      Ray ray = new Ray(origin, direction);
      RaycastHit hit;

      Debug.DrawRay(ray.origin, ray.direction * 5, Color.red, 1);

      if (Physics.Raycast(ray, out hit)) {
         Transform bullet = Instantiate(bulletHole, hit.point, Quaternion.identity);
         bullet.transform.LookAt(hit.normal);
         bullet.GetComponent<NetworkObject>().Spawn();
         if (hit.collider.tag == "Zombie")
            hit.collider.GetComponentInParent<NetworkZombie>().TakeDamages(currentGun.GetDamages(), hit.collider.name);
      }
      ToggleMuzzleFlashServerRpc();
   }

   [ServerRpc(RequireOwnership = false)]
   private void ToggleMuzzleFlashServerRpc() {
      ToggleMuzzleFlashClientRpc();
   }

   [ClientRpc]
   private void ToggleMuzzleFlashClientRpc() {
      muzzleFlash.enabled = true;
      Invoke("UntoggleMuzzleFlashClientRpc", 0.1f);
   }

   [ClientRpc]
   private void UntoggleMuzzleFlashClientRpc() {
      muzzleFlash.enabled = false;
   }
}
