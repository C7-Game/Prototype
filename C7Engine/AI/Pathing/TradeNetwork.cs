using System.Collections.Generic;
using System.Linq;
using System;
using C7GameData;

namespace C7Engine.Pathing {
	public class TradeNetworkSegment {
		public Dictionary<Resource, int> resourceCounts = new();
		public HashSet<Tile> tiles = new();

		public void AddTile(Tile t, Player p) {
			tiles.Add(t);
			if (t.Resource != Resource.NONE && t.OwningPlayer() == p) {
				if (resourceCounts.TryGetValue(t.Resource, out int currentCount)) {
					resourceCounts[t.Resource] = currentCount + 1;
				} else {
					resourceCounts[t.Resource] = 1;
				}
			}
		}
	}

	// A class for calculating the trade networks.
	//
	// The main idea is that calculating the entire empire's trade network once
	// is faster than doing it repeatedly for each city. By doing it once we can
	// do a simple flood fill of the road network, rather than needing to do
	// actual pathfinding between different tiles.
	//
	// TODO: Handle harbors and airports
	// TODO: Account for passing through the borders of civs we're at war with
	// TODO: Invalidate the trade network when war status changes.
	public class TradeNetwork {
		private Dictionary<Player, Dictionary<City, TradeNetworkSegment>> segments = new();

		public TradeNetwork(GameData gameData) {
			foreach (Player p in gameData.players) {
				ComputeTradeNetwork(p);
			}
		}

		private void ComputeTradeNetwork(Player player) {
			HashSet<Tile> seen = new();

			segments[player] = new();
			foreach (City c in player.cities) {
				if (segments[player].ContainsKey(c)) {
					continue;
				}

				// If we don't know about this city yet, start a new network
				// segment and do a flood fill for all roads coming from the
				// city.
				TradeNetworkSegment segment = new();
				segments[player][c] = segment;

				Queue<Tile> toCheck = new();
				toCheck.Enqueue(c.location);
				seen.Add(c.location);

				while (toCheck.Count > 0) {
					Tile x = toCheck.Dequeue();
					segment.AddTile(x, player);

					if (x.cityAtTile != null) {
						segments[player][x.cityAtTile] = segment;
					}

					foreach (Tile n in x.neighbors.Values) {
						if (n.overlays.HasRoad() && !seen.Contains(n)) {
							seen.Add(n);
							toCheck.Enqueue(n);
						}
					}
				}
			}
		}

		// Returns the resources and their counts that are available to the given
		// city.
		public Dictionary<Resource, int> GetResourcesAvailableToCity(Player player, City city) {
			TradeNetworkSegment segment = segments[player][city];
			Dictionary<Resource, int> result = new();
			foreach ((Resource r, int count) in segment.resourceCounts) {
				if (player.KnowsAboutResource(r)) {
					result[r] = count;
				}
			}
			return result;
		}

		public bool HasTradeAccess(Tile t, Player p, Resource r) {
			if (!p.KnowsAboutResource(r)) {
				return false;
			}

			foreach (TradeNetworkSegment segment in segments[p].Values) {
				if (segment.tiles.Contains(t) && segment.resourceCounts.TryGetValue(r, out int count) && count > 0) {
					return true;
				}
			}
			return false;
		}

		public bool ConnectedToCapital(Player p, City c) {
			return segments[p][c] == segments[p][p.cities[0]];
		}
	}
}
