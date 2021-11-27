namespace C7GameData

/*
  This will read a Civ3 sav into C7 native format for immediate use or saving to native JSON save
*/
{
    using System.IO;
    using System.Collections.Generic;
    using QueryCiv3;

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
            c7Save.map = new GameMap();

            // Get save data reader
            byte[] defaultBicBytes = QueryCiv3.Util.ReadFile(defaultBicPath);
    		SavData civ3Save = new QueryCiv3.SavData(QueryCiv3.Util.ReadFile(savePath), defaultBicBytes);

            // Import data
            c7Save.map.numTilesTall = civ3Save.Wrld.Width;
            c7Save.map.numTilesWide = civ3Save.Wrld.Height;
            // c7Save.map.tiles = new List<Tile>();
            foreach (MapTile tile in civ3Save.Tile)
            {
                Civ3ExtraInfo extra = new Civ3ExtraInfo
                {
                    BaseTerrainFileID = tile.BaseTerrainFileID,
                    BaseTerrainImageID = tile.BaseTerrainImageID,                    
                };
                Tile c7Tile = new Tile
                {
                    xCoordinate = tile.X,
                    yCoordinate = tile.Y,
                    ExtraInfo = extra,
                };
                c7Save.map.tiles.Add(c7Tile);
            }
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