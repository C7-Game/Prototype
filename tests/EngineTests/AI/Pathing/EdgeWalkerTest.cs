using System.Collections.Generic;
using System.Linq;
using C7Engine.Pathing;
using C7GameData;
using Xunit;

namespace EngineTests {
	public class WalkerOnLandTest {
		private static MapUnit MakeLandUnit() {
			MapUnit result = new(ID.None("")) {
				unitType = new UnitPrototype() {
					movement = 2,
				},
			};
			result.unitType.categories.Add("Land");
			return result;
		}

		private UnitWalker walker = new(MakeLandUnit());
		private Tile mountain  = new(ID.None("")) {
			baseTerrainType = new() {
				Key = "mountains"
			},
			overlayTerrainType = new() {
				Key = "mountains",
				movementCost = 3
			}
		};
		private Tile hill  = new(ID.None("")) {
			baseTerrainType = new() {
				Key = "hills"
			},
			overlayTerrainType = new() {
				Key = "hills",
				movementCost = 2
			}
		};
		private Tile plains  = new(ID.None("")) {
			baseTerrainType = new() {
				Key = "plains"
			},
			overlayTerrainType = new() {
				Key = "plains",
				movementCost = 1
			}
		};
		private Tile coast  = new(ID.None("")) {
			baseTerrainType = new() {
				Key = "coast"
			},
			overlayTerrainType = new() {
				Key = "coast",
				movementCost = 1
			}
		};

		[Fact]
		void testIgnoresWater() {
			Tile start = hill;

			// Add 3 neighbors, one of which is water.
			start.neighbors[TileDirection.NORTH] = coast;
			start.neighbors[TileDirection.SOUTH] = mountain;
			start.neighbors[TileDirection.WEST] = plains;

			// The water tile should be ignored, and the costs should be correct.
			IEnumerable<Edge<Tile>> edges = walker.getEdges(start);
			Assert.Equal(2, edges.Count());

			Assert.Contains(edges, item => item.current == mountain && item.distanceToCurrent == 1);
			Assert.Contains(edges, item => item.current == plains && item.distanceToCurrent == 1 / 2.0f);
		}

		[Fact]
		void testRoadOnDestinationNotOnStart() {
			Tile start = hill;

			// Set up a neighbor with a road.
			Tile end = plains;
			end.overlays.road = true;
			start.neighbors[TileDirection.NORTH] = end;

			// The road shouldn't matter, since we don't have a road.
			IEnumerable<Edge<Tile>> edges = walker.getEdges(start);
			Assert.Single(edges);
			Assert.Contains(edges, item => item.current == plains && item.distanceToCurrent == 1 / 2.0f);
		}

		[Fact]
		void testRoadOnStartNotOnDestination() {
			Tile start = hill;
			start.overlays.road = true;

			// Set up a neighbor without a road.
			Tile end = plains;
			start.neighbors[TileDirection.NORTH] = end;

			// The road shouldn't matter, since the destination doesn't have a road.
			IEnumerable<Edge<Tile>> edges = walker.getEdges(start);
			Assert.Single(edges);
			Assert.Contains(edges, item => item.current == plains && item.distanceToCurrent == 1 / 2.0f);
		}

		[Fact]
		void testRoadOnStartAndDestination() {
			Tile start = hill;
			start.overlays.road = true;

			// Set up a neighbor with a road.
			Tile end = plains;
			end.overlays.road = true;
			start.neighbors[TileDirection.NORTH] = end;

			// The cost should be adjusted because we both have a road.
			IEnumerable<Edge<Tile>> edges = walker.getEdges(start);
			Assert.Single(edges);
			Assert.Contains(edges, item => item.current == plains && item.distanceToCurrent == 1.0f / 3.0f / 2.0f);
		}
	}

	public class WalkerOnWaterTest {
		private static MapUnit MakeWaterUnit() {
			MapUnit result = new(ID.None("")) {
				unitType = new UnitPrototype() {
					movement = 2,
				},
			};
			return result;
		}

		private Player player = new();
		private MapUnit nonLandUnit = MakeWaterUnit();
		private Tile hill  = new(ID.None("")) {
			baseTerrainType = new() {
				Key = "hills"
			},
			overlayTerrainType = new() {
				Key = "hills",
				movementCost = 2
			}
		};
		private Tile coast  = new(ID.None("")) {
			baseTerrainType = new() {
				Key = "coast"
			},
			overlayTerrainType = new() {
				Key = "coast",
				movementCost = 1
			}
		};
		private Tile sea  = new(ID.None("")) {
			baseTerrainType = new() {
				Key = "sea"
			},
			overlayTerrainType = new() {
				Key = "sea",
				movementCost = 1
			}
		};

		[Fact]
		void testIgnoresLand() {
			UnitWalker walker = new(nonLandUnit);
			Tile start = coast;

			// Add 2 neighbors, one of which is land.
			start.neighbors[TileDirection.NORTH] = hill;
			start.neighbors[TileDirection.SOUTH] = sea;

			// The land tile should be ignored, and the costs should be correct.
			IEnumerable<Edge<Tile>> edges = walker.getEdges(start);
			Assert.Single(edges);

			Assert.Contains(edges, item => item.current == sea && item.distanceToCurrent == 1 / 2.0f);
		}

		[Fact]
		void testLandIncludedIfItHasCityWithSameOwner() {
			UnitWalker walker = new(nonLandUnit);
			nonLandUnit.owner = player;

			Tile start = coast;

			// Set up a neighbor on land with a city of the same owner.
			Tile end = hill;
			end.cityAtTile = new City(Tile.NONE, player, "", ID.None(""));
			start.neighbors[TileDirection.NORTH] = end;

			// The city tile should be included, to allow for canals, and so
			// that ships can go back into harbors.
			//
			// The cost should be 1, despite the city being on a hill. Land
			// movement costs don't make sense to apply to ships.
			IEnumerable<Edge<Tile>> edges = walker.getEdges(start);
			Assert.Single(edges);
			Assert.Contains(edges, item => item.current == hill && item.distanceToCurrent == 1 / 2.0f);
		}

		[Fact]
		void testCityWithDifferentOwnerNotIncluded() {
			UnitWalker walker = new(nonLandUnit);
			Tile start = coast;
			nonLandUnit.owner = player;
			Player otherPlayer =new();

			// Set up a neighbor on land with a city of the same owner.
			Tile end = hill;
			end.cityAtTile = new City(Tile.NONE, otherPlayer, "", ID.None(""));
			start.neighbors[TileDirection.NORTH] = end;

			// The city tile should be included, to allow for canals, and so
			// that ships can go back into harbors.
			//
			// The cost should be 1, despite the city being on a hill. Land
			// movement costs don't make sense to apply to ships.
			IEnumerable<Edge<Tile>> edges = walker.getEdges(start);
			Assert.Equal(edges.Count(), 0);
		}
	}
}
