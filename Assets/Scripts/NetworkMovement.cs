using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class NetworkMovement : NetworkBehaviour {
   [SerializeField] private float speed;
   [SerializeField] private float xSensitivity;
   [SerializeField] private float ySensitivity;
   [SerializeField] private Transform cam;
   private Vector3 movements;
   private float xRotation;
   private float yRotation;
   private Rigidbody rb;

   private void Start() {
      if (IsLocalPlayer)
         cam.GetComponent<Camera>().enabled = true;
      else
         cam.GetComponent<Camera>().enabled = false;

      if (!IsOwner) return;

      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;
      rb = GetComponent<Rigidbody>();
   }
   // Update is called once per frame
   void Update() {
      if (!IsOwner)
         return;
      movements = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
      float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * xSensitivity;
      float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * ySensitivity;

      yRotation += mouseX;
      xRotation -= mouseY;

      xRotation = Mathf.Clamp(xRotation, -90, 90);
   }

   private void FixedUpdate() {
      if (!IsOwner) return;

      rb.MovePosition(transform.position + movements * Time.deltaTime * speed);
      transform.Translate(movements * Time.deltaTime * speed);
      transform.rotation = Quaternion.Euler(0, yRotation, 0);
      cam.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
      //RotateServerRpc(xRotation, yRotation);
   }

   [ServerRpc]
   private void RotateServerRpc(ulong clientID, float xRotation, float yRotation) {
      RotateClientRpc(clientID, xRotation, yRotation);
   }

   [ClientRpc]
   private void RotateClientRpc(ulong clientID, float xRotation, float yRotation) {
      Transform client = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientID).gameObject.transform;

      // on rotate le client pour le serveur
      if (!IsOwner)
         client.GetComponentInChildren<Camera>(true).transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
   }
}
