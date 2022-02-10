namespace C7GameData

/*
  This will read a Civ3 sav into C7 native format for immediate use or saving to native JSON save
*/
{
    using QueryCiv3;
    using QueryCiv3.Biq;
    using System;

    // Additional parameters used to refer to specic media files and tiles in Civ3
    public class Civ3ExtraInfo
    {
        public int BaseTerrainFileID;
        public int BaseTerrainImageID;
    }
    public class ImportCiv3
    {
        static public C7SaveFormat ImportSav(string savePath, string defaultBicPath)
        {
            // init empty C7 save
            C7SaveFormat c7Save = new C7SaveFormat();
            c7Save.GameData = new GameData();
            c7Save.GameData.map = new GameMap();

            // Get save data reader
            byte[] defaultBicBytes = QueryCiv3.Util.ReadFile(defaultBicPath);
    		SavData civ3Save = new QueryCiv3.SavData(QueryCiv3.Util.ReadFile(savePath), defaultBicBytes);

            //Not dummy data.  Import Civ3 terrains.
            foreach (TERR terrain in civ3Save.Bic.Terr) {
                TerrainType c7TerrainType = TerrainType.ImportFromCiv3(terrain);
                c7Save.GameData.terrainTypes.Add(c7TerrainType);
            }

            // Import tiles
            c7Save.GameData.map.numTilesTall = civ3Save.Wrld.Height;
            c7Save.GameData.map.numTilesWide = civ3Save.Wrld.Width;

            int i = 0;
            foreach (QueryCiv3.Sav.TILE civ3Tile in civ3Save.Tile)
            {
                Civ3ExtraInfo extra = new Civ3ExtraInfo
                {
                    BaseTerrainFileID = civ3Tile.TextureFile,
                    BaseTerrainImageID = civ3Tile.TextureLocation,
                };
                int y = i / (civ3Save.Wrld.Width / 2);
                int x = (i % (civ3Save.Wrld.Width / 2)) * 2 + (y % 2);
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
			c7Save.GameData = new GameData();
			c7Save.GameData.map = new GameMap();
			
			byte[] biqBytes = QueryCiv3.Util.ReadFile(biqPath);
			BiqData theBiq = new BiqData(biqBytes);
			
			foreach (TERR terrain in theBiq.Terr) {
				TerrainType c7TerrainType = TerrainType.ImportFromCiv3(terrain);
				c7Save.GameData.terrainTypes.Add(c7TerrainType);
			}
			
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
				int y = i / (theBiq.Wmap[0].Width / 2);
				int x = (i % (theBiq.Wmap[0].Width / 2)) * 2 + (y % 2);
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
    }
}
