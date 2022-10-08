using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGame : MonoBehaviour {
	[SerializeField] TMP_InputField nicknameIF;
	[SerializeField] Button validateBT;
	void Start() {
		if (PlayerPrefs.HasKey("nickname")) {
			Validate();
			return;
		}

		FindObjectOfType<Dialogs>().Prompt(
			new List<(string, Sprite)> {
			 ("Bienvenue dans Conquert Dawn !",Tools.GetSprite("head_officier-pink")),
			 ("Comment vous appellez vous ?", null),
		});
	}

	public void ChangeNick() {
		validateBT.interactable = nicknameIF.text.Length >= 3;
	}

	public void Validate() {
		PlayerPrefs.SetString("nickname", nicknameIF.text);
		FindObjectOfType<Dialogs>().Prompt(
			new List<(string, Sprite)> {
					 ($"Ravie de faire ta connaissance, {PlayerPrefs.GetString("nickname")}", null),
					 ("Je suis Nell, je suis un Général dans l'armée Orange Star.", null),
					 ("Lance une partie depuis le menu !", null)
		}, new System.Action(() => SceneManager.LoadSceneAsync("MainMenu")));
	}
}
