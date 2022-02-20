namespace C7GameData

/*
  This will read a Civ3 sav into C7 native format for immediate use or saving to native JSON save
*/
{
    using QueryCiv3;
    using QueryCiv3.Biq;

    // Additional parameters used to refer to specific media files and tiles in Civ3
    public class Civ3ExtraInfo
    {
        public int BaseTerrainFileID;
        public int BaseTerrainImageID;
    }
    public class ImportCiv3
    {
        public static C7SaveFormat ImportSav(string savePath, string defaultBicPath)
        {
            // init empty C7 save
            C7SaveFormat c7Save = new C7SaveFormat();

            // Get save data reader
            byte[] defaultBicBytes = Util.ReadFile(defaultBicPath);
    		SavData civ3Save = new SavData(Util.ReadFile(savePath), defaultBicBytes);
            BiqData theBiq = civ3Save.Bic;

            ImportCiv3TerrainTypes(theBiq, c7Save);
            SetMapDimensions(theBiq, c7Save);

            // Import tiles.  This is similar to, but different from the BIQ version as tile contents may have changed in-game.
            int i = 0;
            foreach (QueryCiv3.Sav.TILE civ3Tile in civ3Save.Tile)
            {
                Civ3ExtraInfo extra = new Civ3ExtraInfo
                {
                    BaseTerrainFileID = civ3Tile.TextureFile,
                    BaseTerrainImageID = civ3Tile.TextureLocation,
                };
                int x, y;
                (x, y) = GetMapCoordinates(i, civ3Save.Wrld.Width);
                Tile c7Tile = new Tile
                {
                    xCoordinate = x,
                    yCoordinate = y,
                    ExtraInfo = extra,
                    baseTerrainType = c7Save.GameData.terrainTypes[civ3Tile.BaseTerrain],
                    overlayTerrainType = c7Save.GameData.terrainTypes[civ3Tile.OverlayTerrain],
                };
                if (civ3Tile.SnowCapped) {
                    c7Tile.isSnowCapped = true;
                }
                if (civ3Tile.PineForest) {
                    c7Tile.isPineForest = true;
                }
                c7Tile.riverNortheast = civ3Tile.RiverNortheast;
                c7Tile.riverSoutheast = civ3Tile.RiverSoutheast;
                c7Tile.riverSouthwest = civ3Tile.RiverSouthwest;
                c7Tile.riverNorthwest = civ3Tile.RiverNorthwest;
                c7Save.GameData.map.tiles.Add(c7Tile);
                i++;
            }
            // This probably doesn't belong here, but not sure where else to put it
            // c7Save.GameData.map.RelativeModPath = civ3Save.MediaBic.Game[0].ScenarioSearchFolders;
            return c7Save;
        }

		/**
		 * defaultBiqPath is used in case some sections (map, rules, player data) are not
		 * present.
		 */
		static public C7SaveFormat ImportBiq(string biqPath, string defaultBiqPath)
		{
			C7SaveFormat c7Save = new C7SaveFormat();
			
			byte[] biqBytes = Util.ReadFile(biqPath);
			BiqData theBiq = new BiqData(biqBytes);
			
			ImportCiv3TerrainTypes(theBiq, c7Save);
			SetMapDimensions(theBiq, c7Save);
			
			// Import tiles
			c7Save.GameData.map.numTilesTall = theBiq.Wmap[0].Height;
			c7Save.GameData.map.numTilesWide = theBiq.Wmap[0].Width;
			int i = 0;
			foreach (QueryCiv3.Biq.TILE civ3Tile in theBiq.Tile)
			{
				Civ3ExtraInfo extra = new Civ3ExtraInfo
				{
					BaseTerrainFileID = civ3Tile.TextureFile,
					BaseTerrainImageID = civ3Tile.TextureLocation,
				};
				int x, y;
				(x, y) = GetMapCoordinates(i, theBiq.Wmap[0].Width);
				Tile c7Tile = new Tile
				{
					xCoordinate = x,
					yCoordinate = y,
					ExtraInfo = extra,
					baseTerrainType = c7Save.GameData.terrainTypes[civ3Tile.BaseTerrain],
					overlayTerrainType = c7Save.GameData.terrainTypes[civ3Tile.OverlayTerrain],
				};
				if (civ3Tile.SnowCappedMountain) {
					c7Tile.isSnowCapped = true;
				}
				if (civ3Tile.PineForest) {
					c7Tile.isPineForest = true;
				}
				c7Tile.riverNortheast = civ3Tile.RiverConnectionNortheast;
				c7Tile.riverSoutheast = civ3Tile.RiverConnectionSoutheast;
				c7Tile.riverSouthwest = civ3Tile.RiverConnectionSouthwest;
				c7Tile.riverNorthwest = civ3Tile.RiverConnectionNorthwest;
				c7Save.GameData.map.tiles.Add(c7Tile);
				i++;
			}
			// This probably doesn't belong here, but not sure where else to put it
			// c7Save.GameData.map.RelativeModPath = civ3Save.MediaBic.Game[0].ScenarioSearchFolders;
			return c7Save;
		}
		
		static (int, int) GetMapCoordinates(int tileIndex, int mapWidth)
		{
			int y = tileIndex / (mapWidth / 2);
			int x = (tileIndex % (mapWidth / 2)) * 2 + (y % 2);
			return (x, y);
		}

		private static void ImportCiv3TerrainTypes(BiqData theBiq, C7SaveFormat c7Save)
		{
			foreach (TERR terrain in theBiq.Terr) {
				TerrainType c7TerrainType = TerrainType.ImportFromCiv3(terrain);
				c7Save.GameData.terrainTypes.Add(c7TerrainType);
			}
		}

		private static void SetMapDimensions(BiqData theBiq, C7SaveFormat c7Save)
		{
			c7Save.GameData.map.numTilesTall = theBiq.Wmap[0].Height;
			c7Save.GameData.map.numTilesWide = theBiq.Wmap[0].Width;
		}
    }
}
