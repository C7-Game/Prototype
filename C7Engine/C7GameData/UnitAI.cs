using C7GameData;
using C7GameData.AIData;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace C7GameData {
	//Not-fully-fleshed out player unit AI.
	//Right now it's kind of random to just add some appearance
	//of stuff being done.  I.e. it kinda sucks.  But that's okay.
	//It has to start somewhere, right?
	public interface UnitAI {
		enum Result {
			Done,
			InProgress,
			Error,
		};

		public readonly record struct MoveResult(
			UnitAI.Result Result,
			Task<bool>? PendingMove = null
		) {
			public bool IsMoveRequested => PendingMove is not null;

			public static MoveResult MoveRequested(Task<bool> moveTask) =>
				new(UnitAI.Result.InProgress, moveTask);

			public static implicit operator MoveResult(UnitAI.Result result) =>
				new(result);
		}


		public async Task<Result> PlayTurn(Player player, MapUnit unit) {
			while (unit.movementPoints.canMove && !unit.isFortified) {
				MoveResult result = PlayTurnImpl(player, unit);
				if (result == Result.Error || result == Result.Done) {
					return result.Result;
				}
				if (result.IsMoveRequested) {
					bool stillAlive = await result.PendingMove;
					if (!stillAlive) return Result.Error;
				}
			}
			return Result.InProgress;
		}

		// To be implemented by each AI subclass.
		protected abstract MoveResult PlayTurnImpl(Player player, MapUnit unit);

		// Provide a string representation of the current AI plan.
		string SummarizePlan();

		// Do any bookkeeping required when the unit is destroyed. For example,
		// if an escort is destroyed, update the unit being escorted to reflect
		// that it no longer has an escort.
		void UpdateOnDeath();
	}
}
