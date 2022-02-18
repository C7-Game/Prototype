using System;
using System.Collections.Generic;
using C7GameData;
using System.Linq;

namespace C7Engine
{
	public class SettlerLocationAI
	{
		//Figures out where to plant Settlers
		public static Tile findSettlerLocation(Tile start)
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
			ringTwo.Remove(start);	//start probably got added in as a neighbor of ring 1.
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
			Dictionary<Tile, int> scores = AssignTileScores(candidates);

			IOrderedEnumerable<KeyValuePair<Tile, int> > orderedScores = scores.OrderByDescending(t => t.Value);
			//Debugging: Print out scores
			Tile returnValue = null;
			foreach (KeyValuePair<Tile, int> kvp in orderedScores)
			{
				if (returnValue == null) {
					returnValue = kvp.Key;
				}
				Console.WriteLine("Tile " + kvp.Key + " scored " + kvp.Value);
			}
			return returnValue;
		}
		private static Dictionary<Tile, int> AssignTileScores(HashSet<Tile> candidates)
		{

			Dictionary<Tile, int> scores = new Dictionary<Tile, int>();
			foreach (Tile t in candidates) {
				//TODO: Look at whether we can place cities here.  Hard-coded for now.
				if (t.baseTerrainType.Key == "mountains") {
					scores[t] = 0;
					continue;
				}
				if (t.cityAtTile != null || t.GetLandNeighbors().Exists(n => n.cityAtTile != null)) {
					scores[t] = 0;
					continue;
				}
				int score = 0;
				score = score + t.overlayTerrainType.baseFoodProduction * 5;
				score = score + t.overlayTerrainType.baseShieldProduction * 3;
				score = score + t.overlayTerrainType.baseCommerceProduction * 2;
				//For simplicity's sake, I'm only going to look at immediate neighbors here, but
				//a lot more things should be considered over time.
				foreach (Tile nt in t.neighbors.Values) {
					score = score + nt.overlayTerrainType.baseFoodProduction * 5;
					score = score + nt.overlayTerrainType.baseShieldProduction * 3;
					score = score + nt.overlayTerrainType.baseCommerceProduction * 2;
				}
				//TODO: Also look at the next ring out, with lower weights.

				//Prefer hills for defense, and coast for boats and such.
				if (t.baseTerrainType.Key == "hills") {
					score += 10;
				}
				if (t.NeighborsWater()) {
					score += 10;
				}
				//TODO: Exclude locations that are already settled or too close to another civ.
				scores[t] = score;
			}
			return scores;
		}
	}
}
