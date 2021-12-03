namespace C7GameData

/*
  This will read a Civ3 sav into C7 native format for immediate use or saving to native JSON save
*/
{
    using QueryCiv3;
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

            TerrainType hills = new TerrainType();
            hills.name = "Hills";
            hills.baseFoodProduction = 0;
            hills.baseShieldProduction = 1;
            hills.baseCommerceProduction = 0;
            hills.movementCost = 2;

            TerrainType mountain = new TerrainType();
            mountain.name = "Mountain";
            mountain.baseFoodProduction = 0;
            mountain.baseShieldProduction = 1;
            mountain.baseCommerceProduction = 1;
            mountain.movementCost = 3;
            
            TerrainType volcano = new TerrainType();
            volcano.name = "Volcano";
            volcano.baseFoodProduction = 0;
            volcano.baseShieldProduction = 1;
            volcano.baseCommerceProduction = 1;
            volcano.movementCost = 3;

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
                //Not sure how to put this inline with the c7Tile reference, or without duplicating the base terrain logic
                if (tile.OverlayTerrain == 6) {
                    c7Tile.overlayTerrainType = mountain;
                    if (tile.isSnowCapped) {
                        c7Tile.isSnowCapped = true;
                    }
                }
                else if (tile.OverlayTerrain == 5) {
                    c7Tile.overlayTerrainType = hills;
                }
                else if (tile.OverlayTerrain == 9) {
                    //Marsh
                    c7Tile.overlayTerrainType = c7Tile.terrainType;
                }
                else if (tile.OverlayTerrain == 10) {
                    c7Tile.overlayTerrainType = volcano;
                }
                else {
                    c7Tile.overlayTerrainType = c7Tile.terrainType;
                }
                c7Save.GameData.map.tiles.Add(c7Tile);
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
