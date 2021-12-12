namespace C7GameData
{
    using QueryCiv3;
    public class TerrainType
    {
        public string name {get; set; }
        public int baseFoodProduction {get; set; }
        public int baseShieldProduction {get; set; }
        public int baseCommerceProduction {get; set; }
        public int movementCost {get; set; }

        //some stuff about graphics would probably make sense, too

        public bool isHilly() {
            if (name.Equals("Mountain") || name.Equals("Hills") || name.Equals("Volcano")) {
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return name + "(" + baseFoodProduction + ", " + baseShieldProduction + ", " + baseCommerceProduction + ")";
        }

        public static TerrainType NONE = new TerrainType();
        

        public static TerrainType ImportFromCiv3(TerrSection civ3Terrain)
        {
            TerrainType c7Terrain = new TerrainType();
            c7Terrain.name = civ3Terrain.terrainName;
            c7Terrain.baseFoodProduction = civ3Terrain.food;
            c7Terrain.baseShieldProduction = civ3Terrain.shields;
            c7Terrain.baseCommerceProduction = civ3Terrain.commerce;
            c7Terrain.movementCost = civ3Terrain.movementCost;
            return c7Terrain;
        }
    }
}