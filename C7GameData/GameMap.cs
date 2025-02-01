namespace C7GameData {
	using System;
	using System.Collections.Generic;
	/**
	 * The game map, at the top level.
	 */
	public class GameMap {
		// TODO : protect setters while still allowing JSON deserialization
		public int numTilesWide { get; set; }
		public int numTilesTall { get; set; }
		public bool wrapHorizontally, wrapVertically;

		// The terrainNoiseMap is a full width-by-height matrix unlike the normal game map which has only width/2 tiles per row which are staggered.
		// This is kind of a temporary thing. The reason it works this way right now is because I'm just rearranging the generation code from
		// TerrainAsTileMap, eventually we'll want a more complex map generator which probably won't need this var.
		[System.Text.Json.Serialization.JsonIgnore]
		public int[,] terrainNoiseMap;

		public List<TerrainType> terrainTypes = new List<TerrainType>();
		public List<Tile> tiles { get; set; }
		public List<Tile> barbarianCamps = new List<Tile>();

		public GameMap() {
			this.tiles = new List<Tile>();
		}

		public int tileCoordsToIndex(int X, int Y) {
			return Y * numTilesWide / 2 + (Y % 2 == 0 ? X / 2 : (X - 1) / 2);
		}

		public void tileIndexToCoords(int index, out int X, out int Y) {
			int doubleRow = index / numTilesWide;
			int doubleRowRem = index % numTilesWide;
			if (doubleRowRem < numTilesWide / 2) {
				X = 2 * doubleRowRem;
				Y = 2 * doubleRow;
			} else {
				X = 1 + 2 * (doubleRowRem - numTilesWide / 2);
				Y = 2 * doubleRow + 1;
			}
		}

		public void computeNeighbors() {
			foreach (Tile tile in tiles) {
				Dictionary<TileDirection, Tile> neighbors = new Dictionary<TileDirection, Tile>();
				foreach (TileDirection direction in Enum.GetValues(typeof(TileDirection))) {
					neighbors[direction] = tileNeighbor(tile, direction);
				}
				tile.neighbors = neighbors;
			}
		}

		// This method verifies that the conversion between tile index and coords is consistent for all possible valid inputs. It's not called
		// anywhere but I'm keeping it around in case we ever need to work on the conversion methods again.
		public void testTileIndexComputation() {
			for (int Y = 0; Y < numTilesTall; Y++)
				for (int X = Y % 2; X < numTilesWide; X += 2) {
					int rX, rY;
					int index = tileCoordsToIndex(X, Y);
					tileIndexToCoords(index, out rX, out rY);
					if ((rX != X) || (rY != Y))
						throw new Exception(String.Format("Error computing tile index/coords: ({0}, {1}) -> {2} -> ({3}, {4})", X, Y, index, rX, rY));
				}

			for (int i = 0; i < numTilesWide * numTilesTall / 2; i++) {
				int X, Y;
				tileIndexToCoords(i, out X, out Y);
				int ri = tileCoordsToIndex(X, Y);
				if (ri != i)
					throw new Exception(String.Format("Error computing tile index/coords: {0} -> ({1}, {2}) -> {3}", i, X, Y, ri));
			}
		}

		public bool isRowAt(int Y) {
			return wrapVertically || ((Y >= 0) && (Y < numTilesTall));
		}

		public bool isTileAt(int X, int Y) {
			bool evenRow = Y%2 == 0;
			bool XInBounds;
			{
				if (wrapHorizontally)
					XInBounds = true;
				else if (evenRow)
					XInBounds = (X >= 0) && (X <= numTilesWide - 2);
				else
					XInBounds = (X >= 1) && (X <= numTilesWide - 1);
			}
			return XInBounds && isRowAt(Y) && (evenRow ? (X % 2 == 0) : (X % 2 != 0));
		}

		public int wrapTileX(int X) {
			if (wrapHorizontally) {
				int wrappedX = X % numTilesWide;
				return (wrappedX >= 0) ? wrappedX : wrappedX + numTilesWide;
			} else
				return X;
		}

		public int wrapTileY(int Y) {
			if (wrapVertically) {
				int wrappedY = Y % numTilesTall;
				return (wrappedY >= 0) ? wrappedY : wrappedY + numTilesTall;
			} else
				return Y;
		}

		public Tile tileAt(int X, int Y) {
			if (isTileAt(X, Y))
				return tiles[tileCoordsToIndex(wrapTileX(X), wrapTileY(Y))];
			else
				return Tile.NONE;
		}

		/**
		 * Returns the Tile that neighbors the given Tile in a certain direction,
		 * or the NONE tile if there is no neighbor in said direction.
		 **/
		public Tile tileNeighbor(Tile center, TileDirection direction) {
			Tuple<int, int> neighbor = Tile.NeighborCoordinate(center.XCoordinate, center.YCoordinate, direction);
			//TODO: World wrap should also be accounted for.
			return tileAt(neighbor.Item1, neighbor.Item2);
		}

		public delegate int[,] TerrainNoiseMapGenerator(int rng, int width, int height);

		public List<Tile> generateStartingLocations(int num, int minDistBetween) {
			var tr = new List<Tile>();
			for (int n = 0; n < num; n++) {
				bool foundOne = false;
				for (int numTries = 0; (!foundOne) && (numTries < 100); numTries++) {
					var randTile = tiles[GameData.rng.Next(0, tiles.Count)];
					if (randTile.baseTerrainType.isWater() || !randTile.IsAllowCities())
						continue;
					int distToNearestOtherLoc = Int32.MaxValue;
					foreach (var sL in tr) {
						// TODO: This distance calculation is just a placeholder. Eventually we'll need to write an proper
						// function to find the distance between two tiles. This placeholder is not even very accurate, e.g. it
						// would say that a tile and its east neighbor are at distance 2.
						int dist = Math.Abs(sL.XCoordinate - randTile.XCoordinate) + Math.Abs(sL.YCoordinate - randTile.YCoordinate);
						if (dist < distToNearestOtherLoc)
							distToNearestOtherLoc = dist;
					}
					if (distToNearestOtherLoc >= minDistBetween) {
						tr.Add(randTile);
						foundOne = true;
					}
				}
			}
			return tr;
		}

		/**
		 * Temporary method to generate a map. Right now it uses the basic generator passed in all the way from the UI but eventually we'll want to
		 * implement a more sophisticated generator in the engine.
		 **/
		// TerrainType declarations here have been copied to ImportCiv3, and all loaded terrain is set with one of them
		public static GameMap Generate(GameData gameData) {
			TerrainType grassland = new TerrainType();
			grassland.DisplayName = "Grassland";
			grassland.baseFoodProduction = 2;
			grassland.baseShieldProduction = 1; //with only one terrain type, it needs to be > 0
			grassland.baseCommerceProduction = 1;   //same as above
			grassland.movementCost = 1;
			grassland.allowCities = true;

			TerrainType plains = new TerrainType();
			plains.DisplayName = "Plains";
			plains.baseFoodProduction = 1;
			plains.baseShieldProduction = 2;
			plains.baseCommerceProduction = 1;
			plains.movementCost = 1;
			plains.allowCities = true;

			TerrainType coast = new TerrainType();
			coast.DisplayName = "Coast";
			coast.baseFoodProduction = 2;
			coast.baseShieldProduction = 0;
			coast.baseCommerceProduction = 1;
			coast.movementCost = 1;
			coast.allowCities = false;

			GameMap m = new GameMap();
			m.numTilesTall = 80;
			m.numTilesWide = 80;

			// NOTE: The order of terrain types in this array must match the indices produced by terrainGen
			m.terrainTypes.Add(plains);
			m.terrainTypes.Add(grassland);
			m.terrainTypes.Add(coast);

			for (int Y = 0; Y < m.numTilesTall; Y++) {
				for (int X = Y % 2; X < m.numTilesWide; X += 2) {
					Tile newTile = new Tile(gameData.ids.CreateID("tile"));
					newTile.XCoordinate = X;
					newTile.YCoordinate = Y;
					newTile.baseTerrainType = m.terrainTypes[GameData.rng.Next() % m.terrainTypes.Count];
					m.tiles.Add(newTile);
				}
			}
			return m;
		}

		// STATUS 2021-11-26: This noise function is not currently referenced, but it is a very useful
		//  noisemap generator that we will likely use in the future once we start trying
		//  to generate a full-featured map.
		// Inputs: noise field width and height, bool whether noise should smoothly wrap X or Y
		// Actual fake-isometric map will have different shape, but for noise we'll go straight 2d matrix
		// NOTE: Apparently this OpenSimplex implementation doesn't do octaves, including persistance or lacunarity
		//  Might be able to implement them, use https://www.youtube.com/watch?v=MRNFcywkUSA&list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3&index=4 as reference
		// TODO: Parameterize octaves, persistence, scale/period; compare this generator to Godot's
		// NOTE: Godot's OpenSimplexNoise returns -1 to 1; this one seems to be from 0 to 1 like most Simplex/Perlin implementations
		public static double[,] tempMapGenPrototyping(int rng, int width, int height, bool wrapX = true, bool wrapY = false) {
			// TODO: I think my octaves implementation is broken; specifically it needs normalizing I think as additional octaves drive more extreme values
			int octaves = 1;
			double persistence = 0.5;
			// The public domain OpenSiplex implementation always
			//   seems to be 0 at 0,0, so let's offset from it.
			double originOffset = 1000;
			double scale = 0.03;
			double xRadius = (double)width / (System.Math.PI * 2);
			double yRadius = (double)height / (System.Math.PI * 2);
			OpenSimplexNoise noise = new OpenSimplexNoise();
			double[,] noiseField = new double[width, height];

			for (int x = 0; x < width; x++) {
				double oX = originOffset + (scale * x);
				// Set up cX,cY to make one circle as a function of x
				double theta = ((double)x / (double)width) * (System.Math.PI * 2);
				double cX = originOffset + (scale * xRadius * System.Math.Sin(theta));
				double cY = originOffset + (scale * xRadius * System.Math.Cos(theta));
				for (int y = 0; y < height; y++) {
					double oY = originOffset + (scale * y);
					// Set up ycX,ycY to make one circle as a function of y
					double yTheta = ((double)y / (double)height) * (System.Math.PI * 2);
					double ycX = originOffset + (scale * yRadius * System.Math.Sin(yTheta));
					double ycY = originOffset + (scale * yRadius * System.Math.Cos(yTheta));

					// No wrapping, just yoink values at scaled coordinates
					if (!(wrapX || wrapY)) {
						// noiseField[x,y] = noise.Evaluate(oX, oY);
						for (int i = 0; i < octaves; i++) {
							double offset = i * 1.5 * System.Math.Max(width, height) * scale;
							noiseField[x, y] += (octaves - i) * persistence * noise.Evaluate(oX + offset, oY + offset);
						}
						continue;
					}
					// Bi-axis wrapping requires two extra dimensions and circling through each
					if (wrapX && wrapY) {
						for (int i = 0; i < octaves; i++) {
							double offset = i * 1.5 * System.Math.Max(width, height) * scale;
							double a = cX + offset;
							double b = cY + offset;
							double c = ycX + offset;
							double d = ycY + offset;
							noiseField[x, y] += (octaves - i) * persistence * noise.Evaluate(a, b, c, d);
						}
						// Skip the below tests, go to next loop iteration
						continue;
					}
					// Y wrapping as Y increments it instead traces a circle in a third dimension to match up its ends
					if (wrapY) {
						for (int i = 0; i < octaves; i++) {
							double offset = i * 1.5 * System.Math.Max(width, height) * scale;
							double a = ycX + offset;
							double b = ycY + offset;
							double c = oX + offset;
							noiseField[x, y] += (octaves - i) * persistence * noise.Evaluate(a, b, c);
						}
						continue;
					}
					// Similar to Y wrapping
					if (wrapX) {
						for (int i = 0; i < octaves; i++) {
							double offset = i * 1.5 * System.Math.Max(width, height) * scale;
							double a = cX + offset;
							double b = cY + offset;
							double c = oY + offset;
							noiseField[x, y] += (octaves - i) * persistence * noise.Evaluate(a, b, c);
						}
					}
				}
			}
			return noiseField;
		}
	}
}
