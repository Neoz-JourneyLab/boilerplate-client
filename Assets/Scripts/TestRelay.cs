using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TestRelay : MonoBehaviour {

   [SerializeField] private Button hostButton;
   [SerializeField] private Button joinButton;
   [SerializeField] private TMPro.TMP_InputField joinInput;
   private void Awake() {
      hostButton.onClick.AddListener(() => {
         CreateRelay();
         Debug.Log("Host launched");
      });

      joinButton.onClick.AddListener(() => {
         NetworkManager.Singleton.StartClient();
         JoinRelay(joinInput.text);
         Debug.Log("Joining with code " + joinInput.text);
      });
   }

   private async void Start() {
      await UnityServices.InitializeAsync();

      //

      AuthenticationService.Instance.SignedIn += () => {
         Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
      };

      await AuthenticationService.Instance.SignInAnonymouslyAsync();

      //CreateRelay();
   }

   private async void CreateRelay() {
      try {

         Allocation allocation = await RelayService.Instance.CreateAllocationAsync(7);

         string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

         Debug.Log("join code : " + joinCode);

         RelayServerData data = new RelayServerData(allocation, "dtls");
         NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(data);

         NetworkManager.Singleton.StartHost();
      } catch (RelayServiceException e) {
         Debug.Log("Relay creation error --> " + e.Message);
      }
   }

   private async void JoinRelay(string joinCode) {
      try {
         Debug.Log("Joining relay with " + joinCode);

         JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

         RelayServerData data = new RelayServerData(joinAllocation, "dtls");
         NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(data);

         NetworkManager.Singleton.StartClient();
      } catch (RelayServiceException e) {
         Debug.Log("Relay connection error --> " + e.Message);
      }
   }

}
