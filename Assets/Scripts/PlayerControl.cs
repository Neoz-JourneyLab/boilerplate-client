using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControl : MonoBehaviour {
   // Start is called before the first frame update
   public float speed;
   [Range(0, 1)]
   public float cameraOffset;
   public float cameraHeight;

   Vector2 moveDirection = Vector2.zero;
   Vector2 mousePosition = Vector2.zero;
   Vector3 movement;

   Transform camera;

   Rigidbody rb;
   void Start() {
      rb = GetComponent<Rigidbody>();
      camera = Camera.main.transform;
   }

   public void Update() {
      movement = new Vector3(moveDirection.x, 0, moveDirection.y);

      Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cameraHeight));

      camera.position = Vector3.Lerp(new Vector3(transform.position.x, transform.position.y + cameraHeight, transform.position.z), new Vector3(worldPos.x, cameraHeight, worldPos.z), cameraOffset);

      Vector3 pointToLookAt = new Vector3(worldPos.x, transform.position.y, worldPos.z);
      transform.LookAt(pointToLookAt);

      // On envoie les données de rotation au serveur
   }

   // Update is called once per frame
   void FixedUpdate() {
      rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);

      // On envoie les données de position au serveur
   }

   private void OnMove(InputValue value) {
      moveDirection = value.Get() != null ? (Vector2)value.Get() : Vector2.zero;
   }

   private void OnMousePosition(InputValue value) {
      mousePosition = value.Get() != null ? (Vector2)value.Get() : Vector2.zero;
   }


}
