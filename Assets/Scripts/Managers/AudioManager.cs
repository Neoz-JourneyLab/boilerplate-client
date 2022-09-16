using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
   public void PlaySound(AudioSource src) {
      AudioSource newSound = Instantiate(src, transform.position + Vector3.up, Quaternion.identity);
      Destroy(newSound.gameObject, src.clip.length);
   }
}
