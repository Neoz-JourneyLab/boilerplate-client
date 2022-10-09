using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilesManager : MonoBehaviour
{
	public List<Tile> tiles = new List<Tile>();
	public Tilemap decors;
	public Tilemap alpha;

	private void Start() {
		TileMap();
	}

	void TileMap() {
		for (int x = -128; x <= 128; x++) {
			for (int y = -128; y <= 128; y++) {
				var tile =  alpha.GetTile(new Vector3Int(x, y, 0));
				if (tile == null) {
					continue;
				}
				//print(x + "," + y + " : " + tile.name);
				TileType type = (TileType)Enum.Parse(typeof(TileType), tile.name);
				tiles.Add(new Tile() { x = x, y = y, type = type });
			}
		}

	}

	public int Difficulty(int xPos, int yPos) {
		var tile = tiles.FirstOrDefault(t => t.x == xPos && t.y == yPos);
		if (tile == null) return 0;
		if (tile.type == TileType.forest) return 0;
		if (tile.type == TileType.moutain) return 1;
		return 0;
	} 
}

[Serializable]
public class Tile {
	public int x;
	public int y;
	public TileType type;
}

public enum TileType {
	grass = 0,
	moutain = 1,
	forest = 2
}
