namespace C7Engine
{

	using C7GameData;

    /**
     * This class stores references to data that the engine needs between calls from the player.
     * Most obviously this includes a reference to the C7GameData, but it might eventually
     * also include things like keeping track of which networked players are up to date.
	 *
	 * Note that we should NOT store pointers to pieces of the game data here; that will
	 * all be handled within C7GameData.  We just need a pointer to the main, top level
	 * so we don't forget the state of the game after we create it.
     **/
    public class EngineStorage
    {
		internal static GameData gameData {get; set;}
		public static AnimationTracker animTracker = new AnimationTracker();

		/**
		 * Updates the game data pointer to a new set of game data.
		 * This may be a randomly generated map, or data loaded from a scenario
		 * or from a save file.
		 * The engine will no longer have a reference to the old game data.
		 **/
		public static void setGameData(GameData newGameData)
		{
			gameData = newGameData;
		}
    }
}
