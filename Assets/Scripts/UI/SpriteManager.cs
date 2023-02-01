using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SpriteManager : MonoBehaviour
{
	Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

  private void Awake() {
		foreach (var item in Resources.LoadAll("Sprites", typeof(Sprite))) {
			if (sprites.ContainsKey(item.name)) {
				print("conflit : " + item.name);
				continue;
			}
			sprites.Add(item.name, item as Sprite);
		}
  }

	public static Sprite Get(string name) {
		var sman = FindObjectOfType<SpriteManager>();
		if (!sman.sprites.ContainsKey(name)) {
			Debug.Log("No sprite named " + name + " found");
			return null;
		}
		return sman.sprites[name];
	}
}
