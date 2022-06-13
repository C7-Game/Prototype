using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using C7GameData;
using System.Linq;
using C7GameData.AIData;
using Serilog;

namespace C7Engine
{
	public class SettlerLocationAI {
		private static ILogger log = Log.ForContext<SettlerLocationAI>();

		//Eventually, there should be different weights based on whether the AI already
		//has the resource or not (more important to secure ones that they don't have).
		//But since we don't have trade networks yet, for now there's only one value.
		static int STRATEGIC_RESOURCE_BONUS = 20;
		static int LUXURY_RESOURCE_BONUS = 15;

		//Figures out where to plant Settlers
		public static Tile findSettlerLocation(Tile start, Player player) {
			List<City> playerCities = player.cities;
			List<MapUnit> playerUnits = player.units;
			HashSet<Tile> candidates = GetCandidateTiles(start);
			foreach (City city in playerCities) {
				HashSet<Tile> moreCandidates = GetCandidateTiles(city.location);
				candidates.UnionWith(moreCandidates);
			}
			Dictionary<Tile, int> scores = AssignTileScores(start, player, candidates, playerUnits.FindAll(u => u.unitType.name == "Settler"));
			if (scores.Count == 0 || scores.Values.Max() <= 0) {
				return Tile.NONE;	//nowhere to settle
			}

			IOrderedEnumerable<KeyValuePair<Tile, int> > orderedScores = scores.OrderByDescending(t => t.Value);
			log.Debug("Top city location candidates from " + start + ":");
			Tile returnValue = null;
			foreach (KeyValuePair<Tile, int> kvp in orderedScores.Take(5))
			{
				if (returnValue == null) {
					returnValue = kvp.Key;
				}
				if (kvp.Value > 0) {
					log.Debug("  Tile " + kvp.Key + " scored " + kvp.Value);
				}
			}
			return returnValue;
		}
		private static HashSet<Tile> GetCandidateTiles(Tile start)
		{

			//First approach: Swing out from the start tile, searching for valid locations.
			//This is not going to be amazing at first, don't take this as the One True Way.
			//ringOne = direct neighbors of start tile.  Not valid.
			HashSet<Tile> ringOne = new HashSet<Tile>(start.GetLandNeighbors());
			//Ring two is outside the city, but only settled in CxC fashion, which we aren't teaching to
			//the AI here.
			HashSet<Tile> ringTwo = new HashSet<Tile>();
			foreach (Tile t in ringOne) {
				HashSet<Tile> tiles = new HashSet<Tile>(t.GetLandNeighbors());
				ringTwo.UnionWith(tiles);
			}
			ringTwo.ExceptWith(ringOne);
			ringTwo.Remove(start); //start probably got added in as a neighbor of ring 1.
			//Ring three is CxxC style city planning.  Potentially valid.
			HashSet<Tile> ringThree = new HashSet<Tile>();
			foreach (Tile t in ringTwo) {
				ringThree.UnionWith(new HashSet<Tile>(t.GetLandNeighbors()));
			}
			ringThree.ExceptWith(ringTwo);
			ringThree.ExceptWith(ringOne);
			//Ring four is CxxxC style city planning.  Also potentially valid.
			HashSet<Tile> ringFour = new HashSet<Tile>();
			foreach (Tile t in ringThree) {
				ringFour.UnionWith(new HashSet<Tile>(t.GetLandNeighbors()));
			}
			ringFour.ExceptWith(ringThree);
			ringFour.ExceptWith(ringTwo);

			//Okay, we've got our rings.  Now let's try to evaluate them.
			HashSet<Tile> candidates = new HashSet<Tile>();
			candidates.UnionWith(ringThree);
			candidates.UnionWith(ringFour);
			return candidates;
		}
		private static Dictionary<Tile, int> AssignTileScores(Tile startTile, Player player, HashSet<Tile> candidates, List<MapUnit> playerSettlers)
		{

			Dictionary<Tile, int> scores = new Dictionary<Tile, int>();
			foreach (Tile t in candidates) {
				//TODO: Look at whether we can place cities here.  Hard-coded for now.
				if (t.overlayTerrainType.Key == "mountains") {
					scores[t] = 0;
					continue;
				}
				if (t.cityAtTile != null || t.GetLandNeighbors().Exists(n => n.cityAtTile != null)) {
					scores[t] = 0;
					continue;
				}
				foreach (MapUnit otherSettler in playerSettlers)
				{
					if (otherSettler.currentAIData is SettlerAIData otherSettlerAI) {
						if (otherSettlerAI.destination == t) {
							scores[t] = 0;
							goto nextcandidate;	//in Java you can continue based on an outer loop label, but C# doesn't offer that.  So we'll use a beneficial goto instead.
						}
						if (otherSettlerAI.destination.GetLandNeighbors().Exists(n => n == t)) {
							scores[t] = 0;
							goto nextcandidate;
						}
					}
				}
				int score = GetTileYieldScore(t, player);
				//For simplicity's sake, I'm only going to look at immediate neighbors here, but
				//a lot more things should be considered over time.
				foreach (Tile nt in t.neighbors.Values) {
					score += GetTileYieldScore(nt, player);
				}
				//TODO: Also look at the next ring out, with lower weights.

				//Prefer hills for defense, and coast for boats and such.
				if (t.baseTerrainType.Key == "hills") {
					score += 10;
				}
				if (t.NeighborsWater()) {
					score += 10;
				}
				//TODO: Exclude locations that are too close to another civ.

				//Lower scores if they are far away
				int distance = startTile.distanceTo(t);
				if (distance > 4) {
					score -= distance * 2;
				}

				//TODO: Remove locations that we already have another Settler moving towards

				scores[t] = score;
nextcandidate: ;
			}
			return scores;
		}
		private static int GetTileYieldScore(Tile t, Player owner)
		{
			int score = t.foodYield(owner) * 5;
			score += t.productionYield(owner) * 3;
			score += t.commerceYield(owner) * 2;
			if (t.Resource.Category == ResourceCategory.STRATEGIC) {
				score += STRATEGIC_RESOURCE_BONUS;
			}
			else if (t.Resource.Category == ResourceCategory.LUXURY) {
				score += LUXURY_RESOURCE_BONUS;
			}
			return score;
		}
	}
}
