using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DonnerDesOrdres : MonoBehaviour {
	void Start() {
		FindObjectOfType<Dialogs>().Prompt(new List<(string, Sprite)>() {
							("La r�gion d'Alara est assez �loign�e.", Tools.GetSprite("head_officier-pink")),
							("Cela explique le faible nombre de troupes ennemies.", null),
							($"Tu as deux <b>{ColorPalette.GetHex(Palette.red)}Infanteries</b></color> sous tes ordres.", null),
							("Les unit�s oranges sont � toi.", null),
							("Les unit�s bleues sont � notre ennemi. Bats les forces adverse pour remplir ta mission !", null),
							("Bien, apprenons � commander. Touches une de tes unit�s d'infanterie.", null),
						},
				new System.Action(() => SceneManager.LoadSceneAsync(name))
		);
	}
}
