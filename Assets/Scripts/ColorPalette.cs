using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ColorPalette {
	private static readonly Dictionary<Palette, Color> palette = new Dictionary<Palette, Color>()
	{
		{ Palette.paleBlue, GetFromHex("BFE2FF") },
		{ Palette.red, GetFromHex("D41D1D") },
		{ Palette.lime, GetFromHex("7eff25") },
		{ Palette.whatsappGreen, GetFromHex("004A3C") },
		{ Palette.darkRed, GetFromHex("8C0000") },
		{ Palette.whatsappGray, GetFromHex("1A2329") },
		{ Palette.gold, GetFromHex("FFE410") },
		{ Palette.lightGreen, GetFromHex("AAFFAE") },
	};

	public static Color Get(Palette col) {
		return palette[col];
	}

	public static string GetHex(Palette col) {
		return "<#" + ColorUtility.ToHtmlStringRGB(palette[col]) + ">";
	}

	static float HexToFloat(string hex) {
		try {
			return (Convert.ToInt32(hex, 16) / 255f);
		} catch (Exception) {
			Debug.Log("Impossible de récupérer le code : " + hex);
		}
		return 0;
	}

	static Color GetFromHex(string hex) {
		float r = HexToFloat(hex.Substring(0, 2));
		float g = HexToFloat(hex.Substring(2, 2));
		float b = HexToFloat(hex.Substring(4, 2));
		return new Color(r, g, b);
	}
}
public enum Palette {
	paleBlue,
	red,
	lime,
	whatsappGreen,
	whatsappGray,
	darkRed,
	gold,
	lightGreen
}