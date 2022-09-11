using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashLight : MonoBehaviour {
   public GameObject flashlight;
   // Start is called before the first frame update
   void Start() {
      try {
         flashlight = transform.Find("GunHolder/Gun/Flashlight").gameObject;
      } catch (Exception e) {
         print(e.Message);
      }
   }

   // Update is called once per frame
   void Update() {

   }

   void OnFlashlight() {
      flashlight.SetActive(!flashlight.activeSelf);
      uWebSocketManager.EmitEv("flashlight:emit", new { flashlightState = flashlight.activeSelf });
   }
}
