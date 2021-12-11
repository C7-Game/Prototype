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
            TerrainType desert = new TerrainType();
            desert.name = "Desert";
            desert.baseFoodProduction = 0;
            desert.baseShieldProduction = 1;
            desert.baseCommerceProduction = 0;
            desert.movementCost = 1;

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
            
            TerrainType tundra = new TerrainType();
            tundra.name = "Tundra";
            tundra.baseFoodProduction = 1;
            tundra.baseShieldProduction = 0;
            tundra.baseCommerceProduction = 0;
            tundra.movementCost = 1;

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

            TerrainType forest = new TerrainType();
            forest.name = "Forest";
            forest.baseFoodProduction = 1;
            forest.baseShieldProduction = 2;
            forest.baseCommerceProduction = 0;
            
            TerrainType jungle = new TerrainType();
            jungle.name = "Jungle";
            jungle.baseFoodProduction = 1;
            jungle.baseShieldProduction = 0;
            jungle.baseCommerceProduction = 0;
            
            TerrainType marsh = new TerrainType();
            marsh.name = "Marsh";
            marsh.baseFoodProduction = 1;
            marsh.baseShieldProduction = 0;
            marsh.baseCommerceProduction = 0;

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
                    baseTerrainType = tile.BaseTerrain > 10 ? coast : tile.BaseTerrain > 1 ? grassland : plains,
                };
                if (tile.BaseTerrain == 3) {
                    c7Tile.baseTerrainType = tundra;
                }
                else if (tile.BaseTerrain == 2) {
                    c7Tile.baseTerrainType = grassland;
                }
                else if (tile.BaseTerrain == 1) {
                    c7Tile.baseTerrainType = plains;
                }
                else if (tile.BaseTerrain == 0) {
                    c7Tile.baseTerrainType = desert;
                }
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
                else if (tile.OverlayTerrain == 7) {
                    c7Tile.overlayTerrainType = forest;
                    if (tile.isPineForest) {
                        c7Tile.isPineForest = true;
                    }
                }
                else if (tile.OverlayTerrain == 8) {
                    c7Tile.overlayTerrainType = jungle;
                }
                else if (tile.OverlayTerrain == 9) {
                    c7Tile.overlayTerrainType = marsh;
                }
                else if (tile.OverlayTerrain == 10) {
                    c7Tile.overlayTerrainType = volcano;
                }
                else {
                    c7Tile.overlayTerrainType = c7Tile.baseTerrainType;
                }
                c7Save.GameData.map.tiles.Add(c7Tile);
            }
            // This probably doesn't belong here, but not sure where else to put it
            c7Save.GameData.map.RelativeModPath = civ3Save.MediaBic.RelativeModPath;
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
