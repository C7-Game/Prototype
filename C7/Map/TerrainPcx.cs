using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace C7.Map {

	class TerrainPcx {
		private static Random prng = new Random();
		private string name;
		// abc refers to the layout of the terrain tiles in the pcx based on
		// the positions of each terrain at the corner of 4 tiles.
		// - https://forums.civfanatics.com/threads/terrain-editing.622999/
		// - https://forums.civfanatics.com/threads/editing-terrain-pcx-files.102840/
		private string[] abc;
		public int atlas;
		public TerrainPcx(string name, string[] abc, int atlas) {
			this.name = name;
			this.abc = abc;
			this.atlas = atlas;
		}
		public bool validFor(string[] corner) {
			return corner.All(tile => abc.Contains(tile));
		}
		private int abcIndex(string terrain) {
			List<int> indices = new List<int>();
			for (int i = 0; i < abc.Count(); i++) {
				if (abc[i] == terrain) {
					indices.Add(i);
				}
			}
			return indices[prng.Next(indices.Count)];
		}

		// getTextureCoords looks up the correct texture index in the pcx
		// for the given position of each corner terrain type
		public Vector2I getTextureCoords(string[] corner) {
			int top = abcIndex(corner[0]);
			int right = abcIndex(corner[1]);
			int bottom = abcIndex(corner[2]);
			int left = abcIndex(corner[3]);
			int index = top + (left * 3) + (right * 9) + (bottom * 27);
			return new Vector2I(index % 9, index / 9);
		}
	}

}
