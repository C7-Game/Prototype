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
            c7Save.GameData.map.numTilesWide = civ3Save.Wrld.Width;
            c7Save.GameData.map.numTilesTall = civ3Save.Wrld.Height;

            int tileIndex = 0;
            int x, y;
            foreach (TILE civ3Tile in civ3Save.Bic.Tile)
            {
                c7Save.GameData.map.tileIndexToCoords(tileIndex, out x, out y);
                Tile c7Tile = new Tile(c7Save.GameData, x, y, civ3Tile);
                c7Save.GameData.map.tiles.Add(c7Tile);
                tileIndex++;
            }
            // This probably doesn't belong here, but not sure where else to put it
            c7Save.GameData.map.RelativeModPath = civ3Save.MediaBic.Game[0].ScenarioSearchFolders;
            return c7Save;
        }

        // stub
        static public C7RulesFormat ImportBic()
        {
            C7RulesFormat c7Save = new C7RulesFormat();
            return c7Save;
        }
    }
}
