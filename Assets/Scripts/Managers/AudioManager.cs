using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioManager : MonoBehaviour {
   public void PlaySound(AudioSource src) {
      AudioSource newSound = Instantiate(src, transform.position + Vector3.up, Quaternion.identity);
      Destroy(newSound.gameObject, src.clip.length);
   }

  public List<AudioClip> clips = new List<AudioClip>();

  public void Play(string alias) {
		GameObject newSound = Instantiate(new GameObject(), transform.position + Vector3.up, Quaternion.identity);
    newSound.AddComponent<AudioSource>();
    newSound.GetComponent<AudioSource>().PlayOneShot(clips.First(c => c.name == alias));
		Destroy(newSound.gameObject, clips.First(c => c.name == alias).length);
	}
}
