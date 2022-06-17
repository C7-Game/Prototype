using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using C7GameData;
using C7GameData.AIData;
using C7Engine.Pathing;
using Serilog;

namespace C7Engine
{
	public class ExplorerAI : UnitAI {
		private static ILogger log = Log.ForContext<ExplorerAI>();

		public bool PlayTurn(Player player, MapUnit unit)
		{
			ExplorerAIData explorerData = (ExplorerAIData)unit.currentAIData;
			if (MovingToNewExplorationArea(explorerData)) {
				return MoveToNextTileOnPath(explorerData, unit);
			}
			else {
				bool foundNeighboringTileToExplore = ExploreNeighboringTile(player, unit, explorerData);
				if (foundNeighboringTileToExplore) {
					return true;
				}

				//Find the nearest tile that will allow us to continue exploring.
				//We prefer nearest because the one that allows the most discovery might be pretty far away
				bool foundNewPath = FindPathToNewExplorationArea(player, explorerData, unit);
				if (foundNewPath) {
					MoveToNextTileOnPath(explorerData, unit);
					return true;
				}
			}
			return false;
		}

		private static bool MoveToNextTileOnPath(ExplorerAIData explorerData, MapUnit unit) {
			Tile next = explorerData.path.Next();
			foreach (KeyValuePair<TileDirection, Tile> neighbor in unit.location.neighbors) {
				if (neighbor.Value == next) {
					unit.move(neighbor.Key);
					return true;
				}
			}
			//In the future, it might no longer be possible to go to the correct neighbor, perhaps
			//due to another civ's units having moved there.  Thus, this method can return false.
			return false;
		}

		private static bool ExploreNeighboringTile(Player player, MapUnit unit, ExplorerAIData aiData) {
			List<Tile> validNeighboringTiles = unit.unitType.categories.Contains("Sea") ? unit.location.GetCoastNeighbors() : unit.location.GetLandNeighbors();
			if (validNeighboringTiles.Count == 0) {
				log.Information("No valid exploration locations for unit " + unit + " at location " + unit.location);
				return false;
			}
			// log.Verbose($"Exploring for unit {unit}");
			KeyValuePair<Tile, float> topScoringTile = FindTopScoringTileForExploration(player, validNeighboringTiles, aiData.type);
			Tile newLocation = topScoringTile.Key;

			if (newLocation != Tile.NONE && topScoringTile.Value > 0) {
				unit.move(unit.location.directionTo(newLocation));
				return true;
			}
			return false;
		}

		private static bool MovingToNewExplorationArea(ExplorerAIData explorerData) {
			return explorerData.path != null && explorerData.path.PathLength() > 0;
		}

		private static bool FindPathToNewExplorationArea(Player player, ExplorerAIData explorerData, MapUnit unit) {
			Stopwatch watch = new Stopwatch();
			watch.Start();
			List<Tile> validExplorerTiles = new List<Tile>();
			foreach (Tile t in player.tileKnowledge.AllKnownTiles()
					.Where(t => unit.CanEnterTile(t, false) && t.cityAtTile == null && numUnknownNeighboringTiles(player, t) > 0))
			{
				validExplorerTiles.Add(t);
			}

			int CrowFliesDistance(Tile x, Tile y) {
				return x.distanceTo(unit.location) - y.distanceTo(unit.location);
			};

			validExplorerTiles.Sort(CrowFliesDistance);

			if (validExplorerTiles.Count == 0) {
				//Nowhere to explore.
				//TODO: Change unit AI behavior to something else e.g. defender
				return false;
			}

			int lowestDistance = int.MaxValue;
			TilePath chosenPath = null;

			PathingAlgorithm algo = PathingAlgorithmChooser.GetAlgorithm();
			log.Debug("Explorer pathing from " + unit.location + " with " + unit.unitType);
			foreach (Tile t in validExplorerTiles) {
				if (t.distanceTo(unit.location) > lowestDistance) {
					//Impossible to be shorter, skip it
					continue;
				}

				long millis = watch.ElapsedMilliseconds;
				TilePath path = algo.PathFrom(unit.location, t);
				if (path.PathLength() < lowestDistance) {
					lowestDistance = path.PathLength();
					chosenPath = path;
				}

				long elapsedTimeForTile = watch.ElapsedMilliseconds - millis;

				if (elapsedTimeForTile >= 10) {
					log.Warning("Pathing time for {Tile} = {Time} ms", t, elapsedTimeForTile);
				}

				if (lowestDistance == 1) {
					break;
				}
			}

			log.Debug($"Explorer pathing took " + watch.ElapsedMilliseconds + " milliseconds");

			if (chosenPath == null) {
				//This could happen if there is e.g. a land tile that we could explore from, but on a different landmass.
				//Later, we might recruit a boat to take us there, but for now it's a fail state.
				return false;
			}
			explorerData.path = chosenPath;
			return true;
		}

		public static KeyValuePair<Tile, float> FindTopScoringTileForExploration(Player player, IEnumerable<Tile> possibleNewLocations, ExplorerAIData.ExplorationType type)
		{
			//Technically, this should be the *estimated* new tiles revealed.  If a mountain blocks visibility,
			//we won't know that till we move there.
			Dictionary<Tile, float> explorationScore = new Dictionary<Tile, float>();
			foreach (Tile t in possibleNewLocations) {
				int baseScore = numUnknownNeighboringTiles(player, t);
				if (baseScore == 0) {
					explorationScore[t] = 0;
				}
				else if (type == ExplorerAIData.ExplorationType.NEAR_CITIES) {
					if (baseScore == 0) {
						explorationScore[t] = 0;
					}
					int distanceToNearestCity = int.MaxValue;
					foreach (City c in player.cities) {
						int distance = t.distanceTo(c.location);
						if (distance < distanceToNearestCity) {
							distanceToNearestCity = distance;
						}
					}
					explorationScore[t] = 100 - 4 * distanceToNearestCity * distanceToNearestCity + baseScore;
					log.Verbose($"Exploration score for {t}: {explorationScore[t]}");
				} else {
					explorationScore[t] = baseScore;
				}
			}
			IOrderedEnumerable<KeyValuePair<Tile, float>> orderedScores = explorationScore.OrderByDescending(t => t.Value);
			return orderedScores.First();
		}

		private static int numUnknownNeighboringTiles(Player player, Tile t) {
			//Do not try to explore a tile with a city.  If we own it, we know all tiles.
			//If someone else does, that would be war, which is not a scout's job.
			if (t.cityAtTile != null) {
				return 0;
			}
			//Calculate whether it, and its neighbors are in known tiles.
			int discoverableTiles = 0;
			if (!player.tileKnowledge.isTileKnown(t))
			{
				discoverableTiles++;
			}
			foreach (Tile n in t.neighbors.Values)
			{
				if (!player.tileKnowledge.isTileKnown(n))
				{
					discoverableTiles++;
				}
			}
			return discoverableTiles;
		}
	}
}
