using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashLight : MonoBehaviour {
   public GameObject flashlight;
   // Start is called before the first frame update
   void Start() {

   }

   // Update is called once per frame
   void Update() {

   }

   void OnFlashlight() {
      if (flashlight == null)
         flashlight = transform.Find("GunHolder/Gun/Flashlight").gameObject;
      if (flashlight == null)
         return;
      flashlight.SetActive(!flashlight.activeSelf);
   }
}
