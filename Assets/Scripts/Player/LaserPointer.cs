using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserPointer : MonoBehaviour {
   private LineRenderer lr;
   public LayerMask mask;
   void Start() {
      lr = GetComponent<LineRenderer>();
   }

   // Update is called once per frame
   void Update() {
      lr.SetPosition(0, transform.position);
      Ray ray = new Ray(transform.position, transform.forward);
      RaycastHit hit;
      if (Physics.Raycast(ray, out hit, 100, mask, QueryTriggerInteraction.Ignore)) {
         if (hit.collider) {
            lr.SetPosition(1, hit.point);
         }
      } else lr.SetPosition(1, transform.forward * 100);
   }
}
