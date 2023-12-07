using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class NetworkZombie : NetworkBehaviour {
   [SerializeField] private NetworkVariable<int> currentHealth;
   Transform target;
   NavMeshAgent agent;

   [SerializeField] private TextMeshPro healthText;

   public override void OnNetworkSpawn() {
      base.OnNetworkSpawn();
      //currentHealth = new NetworkVariable<int>(100);
      healthText.text = currentHealth.Value.ToString();



      agent = GetComponent<NavMeshAgent>();

      currentHealth.OnValueChanged += UpdateHealth;
      if (IsServer) {
         Debug.Log("is server !");
         InvokeRepeating("FindClosestServerRpc", 0, 0.2f);
      }
   }

   public void TakeDamages(int dmg, string hitArea) {
      if (hitArea == "arm" || hitArea == "leg")
         currentHealth.Value -= dmg / 2;
      else if (hitArea == "torso")
         currentHealth.Value -= dmg;
      else
         currentHealth.Value -= dmg * 2;

      //include point system

      if (currentHealth.Value < 0) {
         DieServerRpc();
      }
   }

   [ServerRpc]
   private void FindClosestServerRpc() {
      target = null;

      float minDistance = -1;
      ulong closestClientId = 123456;
      foreach (var client in NetworkManager.Singleton.ConnectedClients) {

         ulong clientId = client.Key;
         Transform clientTransform = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId).transform;

         print(client.Key + " -- " + client.Value);

         Debug.Log(NetworkManager.Singleton.ConnectedClients);

         float distance = Vector3.Distance(transform.position, clientTransform.position);

         if (minDistance == -1) {
            closestClientId = client.Key;
            minDistance = distance;
            target = clientTransform;
         } else if (distance < minDistance) {
            closestClientId = client.Key;
            minDistance = distance;
            target = clientTransform;
         }

      }
      Debug.Log("Target is " + closestClientId);
      if (target != null)
         UpdateTargetClientRpc(target.position);
   }

   [ClientRpc]
   private void UpdateTargetClientRpc(Vector3 targetPos) {
      agent.SetDestination(targetPos);
   }

   [ServerRpc]
   public void DieServerRpc() {
      GetComponent<NetworkObject>().Despawn();
   }

   private void UpdateHealth(int oldValue, int newValue) {
      healthText.text = currentHealth.Value.ToString();
   }
}
