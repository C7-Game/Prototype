using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Linq;
using System.Reflection;
using System;

namespace C7GameData.Save {

	public class SaveTile {
		public SaveTile() { }

		public SaveTile(Tile tile) {
			x = tile.xCoordinate;
			y = tile.yCoordinate;
			baseTerrain = tile.baseTerrainTypeKey;
			overlayTerrain = tile.overlayTerrainTypeKey;
			if (tile.Resource != Resource.NONE) {
				resource = tile.ResourceKey;
			}
			if (tile.cityAtTile is not null) {
				city = tile.cityAtTile.id;
			}
			foreach (FieldInfo fi in tile.GetType().GetFields()) {
				if (fi.Name.StartsWith("river") && fi.FieldType == typeof(bool) && (bool)fi.GetValue(tile)) {
					features.Add(fi.Name);
				}
			}
			if (tile.hasBarbarianCamp) {
				features.Add("barbarianCamp");
			}
			if (tile.isBonusShield) {
				features.Add("bonusShield");
			}
			if (tile.isSnowCapped) {
				features.Add("snowCapped");
			}
			if (tile.isPineForest) {
				features.Add("pineForest");
			}
			if (tile.overlays.road) {
				overlays.Add("road");
			}
			if (tile.overlays.railroad) {
				overlays.Add("railroad");
			}
		}

		public Tile ToTile(List<TerrainType> terrainTypes, List<City> cities, List<MapUnit> mapUnits, List<Resource> resources) {
			Tile tile = new Tile{
				xCoordinate = x,
				yCoordinate = y,
				baseTerrainTypeKey = baseTerrain,
				baseTerrainType = terrainTypes.Find(tt => tt.Key == baseTerrain),
				overlayTerrainTypeKey = overlayTerrain,
				overlayTerrainType = terrainTypes.Find(tt => tt.Key == overlayTerrain),
				cityAtTile = cities.Find(c => c.id == city),
				hasBarbarianCamp = features.Contains("barbarianCamp"),
				// TODO: load working tile
				ResourceKey = resource is null ? Resource.NONE.Key : resource,
				riverNorth = features.Contains("riverNorth"),
				riverNortheast = features.Contains("riverNortheast"),
				riverEast = features.Contains("riverEast"),
				riverSoutheast = features.Contains("riverSoutheast"),
				riverSouth = features.Contains("riverSouth"),
				riverSouthwest = features.Contains("riverSouthwest"),
				riverWest = features.Contains("riverWest"),
				riverNorthwest = features.Contains("riverNorthwest"),
				isBonusShield = features.Contains("bonusShield"),
				isSnowCapped = features.Contains("snowCapped"),
				isPineForest = features.Contains("pineForest"),
				overlays = new TileOverlays{
					road = overlays.Contains("road"),
					railroad = overlays.Contains("railroad"),
				},
			};

			tile.Resource = tile.ResourceKey == Resource.NONE.Key ? Resource.NONE : resources.Find(r => r.Key == tile.ResourceKey);

			return tile;
		}

		public int x;
		public int y;
		[JsonRequired]
		public string baseTerrain;
		public string overlayTerrain;
		public string resource;
		public ID city;
		public List<string> features = new List<string>();
		public List<string> overlays = new List<string>();
	}

}
