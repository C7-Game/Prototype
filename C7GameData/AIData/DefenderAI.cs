namespace C7GameData.AIData
{
	public class DefenderAI : UnitAI
	{
		//I've been a bit expansive in possible goals here, perhaps some of them should be
		//in other types of AIs.  But all of them are things I might do with a defensive unit in Civ.
		public enum DefenderGoal
		{
			DEFEND_CITY,
			DEFEND_RESOURCE,
			ESCORT_UNITS,			//including settlers and offensive armies
			DEFEND_CHOKEPOINT,
			DEFEND_BORDER,
			DEFEND_TILE,			//e.g. mountains so the enemy can't take them
			ESTABLISH_BEACHHEAD,	//e.g. on a new continent
			PILLAGE_ENEMY_LANDS
		}
		
		public DefenderGoal goal;
		public Tile destination;
	}
}
