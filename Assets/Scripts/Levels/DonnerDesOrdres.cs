using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DonnerDesOrdres : MonoBehaviour {
	void Start() {
		FindObjectOfType<Dialogs>().Prompt(new List<(string, Sprite)>() {
							("La région d'Alara est assez éloignée.", Tools.GetSprite("head_officier-pink")),
							("Cela explique le faible nombre de troupes ennemies.", null),
							($"Tu as deux <b>{ColorPalette.GetHex(Palette.red)}Infanteries</b></color> sous tes ordres.", null),
							("Les unités oranges sont à toi.", null),
							("Les unités bleues sont à notre ennemi. Bats les forces adverse pour remplir ta mission !", null),
							("Bien, apprenons à commander. Touches une de tes unités d'infanterie.", null),
						},
				new System.Action(() => SceneManager.LoadSceneAsync(name))
		);
	}
}
