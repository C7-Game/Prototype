namespace C7GameData.AIData
{
	public class ExplorerAIData : UnitAIData
	{
		public enum ExplorationType
		{
			RANDOM,
			COASTLINE,
			DIRECTIONAL,
			SCOUT_RIVAL,
			ON_A_BOAT
		}
		public ExplorationType type;
        public TilePath path;
		
		public override string ToString()
		{
			return type + " exploration";
		}
	}
}