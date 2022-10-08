using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameModePrefab : MonoBehaviour {
	public bool sub = true;
	public void Click() {
		if (sub) {
			switch (name) {
				case ("Donner des ordres"):
					FindObjectOfType<Dialogs>().Prompt(new List<(string, Sprite)>() {
							("Commencons par un résumé de la situation.", Tools.GetSprite("head_officier-pink")),
							("Nous sommes dans ce grand pays, Orange Star.", null),
							("Le pays à l'est s'appelle Blue Moon.", null),
							("Les deux nation sont en guerre depuis des années.", null),
							($"Voici ta mission, {PlayerPrefs.GetString("nickname")}", null),
						},
						new System.Action(() => SceneManager.LoadSceneAsync(name))
					);
					break;
			}
			SceneManager.LoadSceneAsync(name);
			return;
		}
		FindObjectOfType<MainMenu>().SetSub(name);
	}
}
