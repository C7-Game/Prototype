using System.Collections.Generic;

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

			//Import Civ3 resources
			int g = 0;
			Dictionary<int, Resource> resourcesByIndex = new Dictionary<int, Resource>(); //will we want to have this for reference later?  Maybe.
			resourcesByIndex[-1] = Resource.NONE;
			foreach (GOOD good in civ3Save.Bic.Good) {
				Resource resource = new Resource
				{
					Index = g,
					Icon = good.Icon,
					FoodBonus = good.FoodBonus,
					ShieldsBonus = good.ShieldsBonus,
					CommerceBonus = good.CommerceBonus,
					AppearanceRatio = good.AppearanceRatio,
					DisappearanceRatio = good.DisappearanceProbability,
					CivilopediaEntry = good.CivilopediaEntry,
				};
				switch (good.Type) {
					case 0:
						resource.Category = ResourceCategory.BONUS;
						break;
					case 1:
						resource.Category = ResourceCategory.LUXURY;
						break;
					case 2:
						resource.Category = ResourceCategory.STRATEGIC;
						break;
					default:
						Console.WriteLine("WARNING!  Unknown resource category for " + good);
						resource.Category = ResourceCategory.NONE;
						break;
				}
				//TODO: Technologies, once they exist
				
				c7Save.GameData.Resources.Add(resource);
				resourcesByIndex[g] = resource;
				g++;
			}
            
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

                c7Tile.Resource = resourcesByIndex[civ3Tile.ResourceID];
                
                c7Save.GameData.map.tiles.Add(c7Tile);
                i++;
            }
            // This probably doesn't belong here, but not sure where else to put it
            // c7Save.GameData.map.RelativeModPath = civ3Save.MediaBic.Game[0].ScenarioSearchFolders;
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
