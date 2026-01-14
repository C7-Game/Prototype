using C7GameData;
using Serilog;
using C7Engine.Pathing;
using System.Threading.Tasks;
using static C7GameData.UnitAI;

namespace C7Engine {
	// This goofy class only exists because of the arbitrary separation between
	// C7GameData and C7Engine.
	public static class UnitAiExtension {
		private static ILogger log = Log.ForContext<UnitAI>();

		// Attempts to move the supplied unit along the given path.
		//
		// `path` is a ref so that the path can be recalculated if necessary.
		public static MoveResult TryToMoveAlongPath(this UnitAI unitAi, MapUnit unit, ref TilePath path, bool allowCombat) {
			if (!unit.movementPoints.canMove) {
				return UnitAI.Result.InProgress;
			}
			Tile nextTile = path.Next();
			if (nextTile == Tile.NONE || !unit.CanEnterTile(nextTile, allowCombat)) {
				log.Information($"Attempting to repath {unit} from {unit.location} to {path.destination}");
				// Attempt to repath. If we succeed, return inprogress so we get
				// called again.
				path = PathingAlgorithmChooser.GetAlgorithm(unit).PathFrom(unit.location, path.destination, unit);
				if ((path?.PathLength() ?? -1) == -1 || path.PeekNext() == Tile.NONE || !unit.CanEnterTile(path.PeekNext(), allowCombat)) {
					return UnitAI.Result.Error;
				}

				return UnitAI.Result.InProgress;
			}

			Task<bool> moveTask = unit.move(unit.location.directionTo(nextTile));

			return MoveResult.MoveRequested(moveTask);
		}
	}
}
