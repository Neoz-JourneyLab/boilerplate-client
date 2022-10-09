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
							("La région d'Alara est assez éloignée.", Tools.GetSprite("head_officier-pink")),
							("Cela explique le faible nombre de troupes ennemies.", null),
							($"Tu as deux <b>{ColorPalette.GetHex(Palette.unit_red)}Infanteries</b></color> sous tes ordres.", null),
							("Les unités oranges sont à toi.", null),
							("Les unités bleues sont à notre ennemi. Bats les forces adverse pour remplir ta mission !", null),
							("Bien, apprenons à commander. Touches une de tes unités d'infanterie.", null),
						});
	}



	bool first = true;
	public void FirstSelect() {
		if (!first) return;
		first = false;
		FindObjectOfType<Dialogs>().Prompt(new List<(string, Sprite)>() {
							("L'action que tu viens d'accomplir est appellée <b>Sélectionner</b>.", Tools.GetSprite("head_officier-pink")),
							("Quand tu sélectionnes une unité, la zone l'entourant s'éclaire.", null),
							("Cette zone représente l'aire de mouvement de l'unité.", null),
							("Tout d'abord, approchons l'ennemi avec cette unité.", null),
							($"Déplace l'unité vers <b>{ColorPalette.GetHex(Palette.unit_blue)}l'infanterie ennemie</b></color> !", null),
						});
	}

	bool firstMove = true;
	public void FirstMove() {
		if (!firstMove) return;
		firstMove = false;
		FindObjectOfType<Dialogs>().Prompt(new List<(string, Sprite)>() {
							("Après que l'unité s'est déplacée, ce menu apparaîtra.", null),
							("Selectionne <b>Attendre</b> pour valider le mouvement.", null),
						});
	}


	bool firstValidate = true;
	public void FirstValidate() {
		if (!firstValidate) return;
		firstValidate = false;
		FindObjectOfType<Dialogs>().Prompt(new List<(string, Sprite)>() {
							("La couleur de l'unité a changé ?", null),
							("Cela indique qu'elle ne peut plus recevoir d'ordres pour ce tour ci.", null),
							("Ne t'inquiète pas, tu pourras t'en servir au prochain tour.", null),
							($"Ok ! Sers toi de la même commande pour déplacer l'autre <b>{ColorPalette.GetHex(Palette.unit_red)}Infanterie</b></color> !", null),
						});
	}
}


