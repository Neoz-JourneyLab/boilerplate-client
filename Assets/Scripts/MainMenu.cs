using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

public class MainMenu : MonoBehaviour {
	public List<GameModes> modes = new List<GameModes>() {
		new GameModes(){
			alias = "Entraînement",
			subMissions = new List<string>(){"Donner des ordres",  "Renseignement", "Capturer une base", "Réparer une unité"} ,
			color = ColorPalette.Get(Palette.lightGreen)
		},
		new GameModes(){
			alias = "VOID",
			subMissions = new List<string>(){"a",  "B", "C", "d"} ,
			color = ColorPalette.Get(Palette.whatsappGreen)
		}
	};

	[SerializeField] GameObject modesPanel;
	[SerializeField] GameObject modePrefab;
	[SerializeField] GameObject subMissionPanel;

	private void Start() {
		foreach (var item in modes) {
			GameObject modeGO = Instantiate(modePrefab, modesPanel.transform);
			modeGO.name = item.alias;
			modeGO.GetComponent<GameModePrefab>().sub = false;
			modeGO.transform.Find("Txt").GetComponent<TMP_Text>().text = item.alias;
			modeGO.GetComponent<Image>().color = item.color;
		}
	}

	public void SetSub(string type) {
		foreach (Transform children in subMissionPanel.transform) {
			Destroy(children.gameObject);
		}
		foreach (var item in modes.First(m => m.alias == type).subMissions) {
			GameObject modeGO = Instantiate(modePrefab, subMissionPanel.transform);
			modeGO.name = item;
			modeGO.transform.Find("Txt").GetComponent<TMP_Text>().text = item;
			modeGO.GetComponent<Image>().color = ColorPalette.Get(Palette.paleBlue);
		}
	}
}

public class GameModes {
	public string alias;
	public List<string> subMissions;
	public Color color;
}
