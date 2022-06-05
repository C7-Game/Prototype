using C7GameData;
using C7GameData.AIData;

namespace C7Engine.AI.StrategicAI {
	public class ExplorationPriority : StrategicPriority {
		private readonly int TEMP_GAME_LENGTH = 540;

		public ExplorationPriority() {
			key = "Exploration";
			this.data.priorityKey = "Exploration";
		}

		public override void CalculateWeightAndMetadata(Player player) {
			//Eventually this should consider the expected number of unknown tiles and the ability to explore them
			//For now this is somewhat placeholder, and one of very few options.
			int gameTurn = EngineStorage.gameData.turn;
			int percentOfGameFinished = (gameTurn * 100) / TEMP_GAME_LENGTH;

			this.calculatedWeight = 100 - 2 * percentOfGameFinished;
		}
	}
}
