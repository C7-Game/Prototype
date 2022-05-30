using System.Collections.Generic;
using C7Engine;

namespace C7GameData.AIData {
	public class ExplorationPriority : StrategicPriority {
		private readonly int TEMP_GAME_LENGTH = 540;

		public Dictionary<float, Dictionary<string, string>> GetWeight(Player player) {
			Dictionary<float, Dictionary<string, string>> returnValue = new Dictionary<float, Dictionary<string, string>>();

			//Eventually this should consider the expected number of unknown tiles and the ability to explore them
			//For now this is somewhat placeholder, and one of very few options.
			int gameTurn = EngineStorage.gameData.turn;
			int percentOfGameFinished = (gameTurn * 100) / TEMP_GAME_LENGTH;
			returnValue[100 - 2 * percentOfGameFinished] = new Dictionary<string, string>();
			return returnValue;
		}
	}
}
