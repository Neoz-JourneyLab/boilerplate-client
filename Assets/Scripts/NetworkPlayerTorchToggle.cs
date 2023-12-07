using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Networking.Transport;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkPlayerTorchToggle : NetworkBehaviour{
   public GameObject lamp;
   void Update() {
      if (!IsOwner)
         return;

      if (Input.GetKeyDown(KeyCode.F))
         ToggleLampServerRpc(OwnerClientId);
   }

   [ServerRpc]
   public void ToggleLampServerRpc(ulong id) {
      ToggleLampClientRpc(id);
   }

   [ClientRpc]
   public void ToggleLampClientRpc(ulong id) {
      /*
      Transform clientPrefab = NetworkManager.SpawnManager.GetPlayerNetworkObject(id).gameObject.transform;

      Debug.Log("Client prefab from " + id + " is " + clientPrefab.name);
      Light light = GetComponentInChildren<Light>();
      light.enabled = !light.enabled;
      */

      lamp.SetActive(!lamp.activeSelf);
   }
}
