using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour {
   [SerializeField] private Button hostBtn;
   [SerializeField] private Button clientBtn;

   private void Awake() {
      hostBtn.onClick.AddListener(() => {
         NetworkManager.Singleton.StartHost();
         Debug.Log("Host launched");
      });

      clientBtn.onClick.AddListener(() => {
         NetworkManager.Singleton.StartClient();
         Debug.Log("Client logged");
      });
   }
}
