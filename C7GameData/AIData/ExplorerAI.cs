namespace C7GameData.AIData
{
	public class ExplorerAI : UnitAI
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
		
		public override string ToString()
		{
			return type + " exploration";
		}
	}
}
