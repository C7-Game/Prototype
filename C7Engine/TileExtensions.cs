using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace C7Engine {
	using C7GameData;

	public static class TileExtensions {
		public static MapUnit FindTopDefender(this Tile tile, MapUnit opponent) {
			if (tile.unitsOnTile.Count > 0) {
				IEnumerable<MapUnit> potentialDefenders = tile.unitsOnTile.Where(u => u.CanDefendAgainst(opponent));
				if (potentialDefenders.Count() == 0) {
					return MapUnit.NONE;
				}

				MapUnit leadingCandidate = tile.unitsOnTile[0];
				foreach (MapUnit u in tile.unitsOnTile)
					if (u.HasPriorityAsDefender(leadingCandidate, opponent))
						leadingCandidate = u;
				return leadingCandidate;
			} else
				return MapUnit.NONE;
		}

		/// <summary>
		/// Disbands non-defending units on a tile.  This should only be called when all defending units have been destroyed,
		/// hence its name.  E.g. if only air/sea units remain after a land battle, this should be called.
		///
		/// Eventually, we should also have a method to make relevant units (workers, artillery, etc.) be captured.
		/// </summary>
		/// <param name="tile"></param>
		public static void DisbandNonDefendingUnits(this Tile tile) {
			//There may have been naval units, if so, disband them
			if (tile.unitsOnTile.Count > 0) {
				//Copy to a separate array so we don't crash due to concurrent modification exceptions
				MapUnit[] unitsOnTile = new MapUnit[tile.unitsOnTile.Count];
				tile.unitsOnTile.CopyTo(unitsOnTile);
				foreach (MapUnit destroyedUnit in unitsOnTile) {
					destroyedUnit.disband();
				}
			}
		}

		/// <summary>
		/// After a new WorkerJob has started, checks for workers working on different jobs and resets them
		/// </summary>
		/// <param name="tile">the current tile</param>
		/// <param name="currentWorkerJob">the worker job currently started</param>
		/// <returns>total progress towards that workerjob, must not be null</returns>
		public static int AddTotalProgressAndResetOtherJobs(this Tile tile, string currentWorkerJob) {

			int totalProgress = 0;
			foreach (MapUnit unit in tile.unitsOnTile) {
				if (unit.WorkerJob == null) {
					continue;
				}

				if (currentWorkerJob.Equals(unit.WorkerJob)) {
					totalProgress += unit.WorkerProgressTowardsJob;
				} else {
					// reset Unit working on other jobs
					unit.resetWorkerJob();
				}
			}
			return totalProgress;
		}

		/// <summary>
		/// After a new WorkerJob has started, checks for workers working on different jobs and resets them
		/// </summary>
		/// <param name="tile">the current tile</param>
		/// <param name="currentWorkerJob">the worker job currently started, nust not be null</param>
		public static void UpdateAllWorkerJobs(this Tile tile, string currentWorkerJob) {
			int totalProgress = 0;
			foreach (MapUnit unit in tile.unitsOnTile) {
				if (unit.WorkerJob == null) {
					continue;
				}

				if (currentWorkerJob==(unit.WorkerJob)) {
					if (unit.movementPoints.canMove) {
						unit.updateWorkerJob();
					}
					totalProgress += unit.WorkerProgressTowardsJob;
				} else {
					// reset Unit working on other jobs
					Log.Error($"Workers working om different WorkerJobs on the same tile");
					unit.resetWorkerJob();
				}
			}

			if (tile.IsWorkerJobFinished(currentWorkerJob, totalProgress)) {
				tile.FinishWorkerJob(currentWorkerJob);
			}
		}

		/// <summary>
		/// After a WorkerJob has finished, Cclean up all the WorkerJobs and set the correct overlay
		/// </summary>
		/// <param name="tile">the current tile</param>
		/// <param name="currentWorkerJob">the worker job currently finished, must not be null</param>
		public static void FinishWorkerJob(this Tile tile, string currentWorkerJob) {
			// Reset All Workers working on the finished Job
			foreach (MapUnit unit in tile.unitsOnTile) {
				if (currentWorkerJob==(unit.WorkerJob)) {
					unit.resetWorkerJob();
				}
			}
			// Set the correct Overlay
			switch (currentWorkerJob) {
				case C7Action.UnitIrrigate:
					tile.overlays.irrigation = true;
					break;
			}
		}

		public static bool IsWorkerJobFinished(this Tile tile, string currentWorkerJob, int totalProgress)
		{
			//TODO Make that dynamic
			int requiredProgress = MapUnitExtensions.JOB_COST_IRRIGATION;

			return totalProgress >= requiredProgress;
		}

		public static void Animate(this Tile tile, AnimatedEffect effect, bool wait) {
			if (EngineStorage.animationsEnabled) {
				new MsgStartEffectAnimation(tile, effect, wait ? EngineStorage.uiEvent : null, AnimationEnding.Stop).send();
				if (wait) {
					EngineStorage.gameDataMutex.ReleaseMutex();
					EngineStorage.uiEvent.WaitOne();
					EngineStorage.gameDataMutex.WaitOne();
				}
			}
		}
	}
}
