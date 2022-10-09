using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class DonnerDesOrdres : MonoBehaviour {
	void Start() {
		FindObjectOfType<Dialogs>().Prompt(new List<(string, Sprite)>() {
							("La r�gion d'Alara est assez �loign�e.", Tools.GetSprite("head_officier-pink")),
							("Cela explique le faible nombre de troupes ennemies.", null),
							($"Tu as deux <b>{ColorPalette.GetHex(Palette.unit_red)}Infanteries</b></color> sous tes ordres.", null),
							("Les unit�s oranges sont � toi.", null),
							("Les unit�s bleues sont � notre ennemi. Bats les forces adverse pour remplir ta mission !", null),
							("Bien, apprenons � commander. Touches une de tes unit�s d'infanterie.", null),
						});
	}



	bool first = true;
	public void FirstSelect() {
		if (!first) return;
		first = false;
		FindObjectOfType<Dialogs>().Prompt(new List<(string, Sprite)>() {
							("L'action que tu viens d'accomplir est appell�e <b>S�lectionner</b>.", Tools.GetSprite("head_officier-pink")),
							("Quand tu s�lectionnes une unit�, la zone l'entourant s'�claire.", null),
							("Cette zone repr�sente l'aire de mouvement de l'unit�.", null),
							("Tout d'abord, approchons l'ennemi avec cette unit�.", null),
							($"D�place l'unit� vers <b>{ColorPalette.GetHex(Palette.unit_blue)}l'infanterie ennemie</b></color> !", null),
						});
	}

	bool firstMove = true;
	public void FirstMove() {
		if (!firstMove) return;
		firstMove = false;
		FindObjectOfType<Dialogs>().Prompt(new List<(string, Sprite)>() {
							("Apr�s que l'unit� s'est d�plac�e, ce menu appara�tra.", null),
							("Selectionne <b>Attendre</b> pour valider le mouvement.", null),
						});
	}


	bool firstValidate = true;
	public void FirstValidate() {
		if (!firstValidate) return;
		firstValidate = false;
		FindObjectOfType<Dialogs>().Prompt(new List<(string, Sprite)>() {
							("La couleur de l'unit� a chang� ?", null),
							("Cela indique qu'elle ne peut plus recevoir d'ordres pour ce tour ci.", null),
							("Ne t'inqui�te pas, tu pourras t'en servir au prochain tour.", null),
							($"Ok ! Sers toi de la m�me commande pour d�placer l'autre <b>{ColorPalette.GetHex(Palette.unit_red)}Infanterie</b></color> !", null),
						});
	}
}


