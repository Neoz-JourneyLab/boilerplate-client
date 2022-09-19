using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Lobby : MonoBehaviour {
	[SerializeField] TMP_InputField gameIF;
	[SerializeField] TMP_InputField nicknameIF;
	[SerializeField] Button gameBT;
	[SerializeField] GameObject gamePrefab;
	[SerializeField] GameObject gameScroll;
	[SerializeField] TMP_Dropdown dropdown;
	List<Game> games = new List<Game>();

	private void Start() {
		if (PlayerPrefs.HasKey("nickname")) {
			nicknameIF.text = PlayerPrefs.GetString("nickname");
		}
		StartCoroutine(nameof(RequestGames));
		//SceneManager.LoadScene("Les catacombes de Markus");
	}
	
	public void LoadGame(string id) {
		Game g = games.Find(g => g.id == id);
		SceneManager.LoadScene(g.name);
	}

	IEnumerator RequestGames() {
		while (string.IsNullOrWhiteSpace(uWebSocketManager.socketId)) yield return new WaitForSeconds(0.2f);
		uWebSocketManager.EmitEv("request:games");
		if(nicknameIF.text != "") {
			uWebSocketManager.EmitEv("nickname", new { nicknameIF.text });
		}
	}

	public void NewGame(string name, string nickname, string id, string level) {
		GameObject game = Instantiate(gamePrefab, gameScroll.transform);
		game.transform.Find("GameName").GetComponent<TMP_Text>().text = name + $" ({nickname}) - {level}";
		game.name = id;

		if (nickname == PlayerPrefs.GetString("nickname")) {
			game.transform.Find("Join").transform.Find("txt").GetComponent<TMP_Text>().text = "Annuler";
			nicknameIF.interactable = false;
			gameIF.interactable = false;
		} else {
			game.transform.Find("GameName").GetComponent<TMP_Text>().color = new Color(0.75f, 0.95f, 0);
		}
		Game g = games.Find(g => g.nickname == nickname);
		if (g != null) {
			Destroy(GameObject.Find(g.id));
			g.id = id;
			g.nickname = nickname;
			g.name = name;
		} else {
			games.Add(new Game() { id = id, nickname = nickname, name = name });
		}
	}

	public void CancelOrJoin(string id) {
		if (games.Find(g => g.id == id).nickname == nicknameIF.text) {
			nicknameIF.interactable = true;
			gameIF.interactable = true;
			uWebSocketManager.EmitEv("cancel:game", new { id });
		} else {
			//join game
		}
	}

	public void CancelGame(string id) {
		games.Remove(games.Find(g => g.id == id));
		Destroy(GameObject.Find(id));
	}

	public void OpenGame() {
		uWebSocketManager.EmitEv("create:game", new { name = gameIF.text, nickname = nicknameIF.text, level = dropdown.captionText.text });
		gameIF.text = "";
		PlayerPrefs.SetString("nickname", nicknameIF.text);
	}

	public void SetOpenGameBT() {
		gameBT.interactable = gameIF.text != "" && nicknameIF.text != "";
	}
}

class Game {
	public string id;
	public string nickname;
	public string name;
}
