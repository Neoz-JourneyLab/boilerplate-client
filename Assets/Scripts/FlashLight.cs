using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashLight : MonoBehaviour {
   public GameObject flashlight;
   [Range(0, 1)]
   public float batteryPercent;
   public float batteryDuration;

   public float maxIntensity;
   public float minIntensity;
   public float maxRange;
   public float minRange;

   // Start is called before the first frame update
   void Start() {
      try {
         flashlight = transform.Find("Flashlight").gameObject;
      } catch (Exception e) {
         print(e.Message);
      }
   }

   // Update is called once per frame
   void Update() {
      if (flashlight == null)
         return;

      if (flashlight.activeSelf) {
         Light light = flashlight.GetComponent<Light>();
         light.intensity = Mathf.Lerp(minIntensity, maxIntensity, batteryPercent);
         light.range = Mathf.Lerp(minRange, maxRange, batteryPercent);
         if (batteryPercent > 0)
            batteryPercent -= (1f / batteryDuration) * Time.deltaTime;
      }
   }

   public void ChangeFlashlight() {
      if (flashlight == null)
         return;
      flashlight.SetActive(!flashlight.activeSelf);
      uWebSocketManager.EmitEv("flashlight:emit", new { flashlightState = flashlight.activeSelf });
   }

   public void RefillLight() {
      batteryPercent = 1;
   }
}
