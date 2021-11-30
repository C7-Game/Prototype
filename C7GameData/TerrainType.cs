namespace C7GameData
{
    public class TerrainType
    {
        public string name {get; set; }
        public int baseFoodProduction {get; set; }
        public int baseShieldProduction {get; set; }
        public int baseCommerceProduction {get; set; }
        public int movementCost {get; set; }

        //some stuff about graphics would probably make sense, too

        public override string ToString()
        {
            return name + "(" + baseFoodProduction + ", " + baseShieldProduction + ", " + baseCommerceProduction + ")";
        }
    }
}