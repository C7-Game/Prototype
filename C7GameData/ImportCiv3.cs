namespace C7GameData

/*
  This will read a Civ3 sav into C7 native format for immediate use or saving to native JSON save
*/
{
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
            c7Save.GameData = new GameData();
            c7Save.GameData.map = new GameMap();

            // Get save data reader
            byte[] defaultBicBytes = QueryCiv3.Util.ReadFile(defaultBicPath);
    		SavData civ3Save = new QueryCiv3.SavData(QueryCiv3.Util.ReadFile(savePath), defaultBicBytes);

            // Dummy data
            TerrainType grassland = new TerrainType();
            grassland.name = "Grassland";
            grassland.baseFoodProduction = 2;
            grassland.baseShieldProduction = 1; //with only one terrain type, it needs to be > 0
            grassland.baseCommerceProduction = 1;   //same as above
            grassland.movementCost = 1;

            TerrainType plains = new TerrainType();
            plains.name = "Plains";
            plains.baseFoodProduction = 1;
            plains.baseShieldProduction = 2;
            plains.baseCommerceProduction = 1;
            plains.movementCost = 1;

            TerrainType coast = new TerrainType();
            coast.name = "Coast";
            coast.baseFoodProduction = 2;
            coast.baseShieldProduction = 0;
            coast.baseCommerceProduction = 1;
            coast.movementCost = 1;

            // Import data
            c7Save.GameData.map.numTilesTall = civ3Save.Wrld.Width;
            c7Save.GameData.map.numTilesWide = civ3Save.Wrld.Height;
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
                    // TEMP all water is coast, desert and plains are plains, grass and tundra are grass
                    terrainType = tile.BaseTerrain > 10 ? coast : tile.BaseTerrain > 1 ? grassland : plains,
                };
                c7Save.GameData.map.tiles.Add(c7Tile);
            }

            // BIC import
            // NOTE: QueryCiv3 now has MediaBic and Bic which may or may not be the same
            //  if a mod has no rules we still need the default Bic for rules, so 
            //  grab the RelativeModPath from MediaBic if present, and the rules from Bic
            c7Save.Rules = ImportBic(civ3Save.MediaBic);
            return c7Save;
        }
        
        // stub
        static public C7RulesFormat ImportBic(BicData bic)
        {
            C7RulesFormat c7Save = new C7RulesFormat
            {
                Civ3RelModPath = bic.RelativeModPath,
            };
            return c7Save;
        }
    }
}
