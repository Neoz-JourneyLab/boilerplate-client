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
		{ Palette.messageBlue, GetFromHex("193F61") },
		{ Palette.darkRed, GetFromHex("8C0000") },
		{ Palette.gray, GetFromHex("1A2329") },
		{ Palette.gold, GetFromHex("FFE410") },
		{ Palette.paleGreen, GetFromHex("B4FDBC") },
		{ Palette.lightGray, GetFromHex("B3B3B3") },
		{ Palette.intenseGreen, GetFromHex("00EF1B") },
		{ Palette.paleOrange, GetFromHex("FF9339") },
	};

	public static Color Get(Palette col, float alpha = 1) {
		Color c = palette[col];
		return new Color(c.r, c.g, c.b, alpha);
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

	public static Color GetFromHex(string hex) {
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
	messageBlue,
	gray,
	darkRed,
	paleGreen,
	lightGray,
	intenseGreen,
	paleOrange,
	gold
}