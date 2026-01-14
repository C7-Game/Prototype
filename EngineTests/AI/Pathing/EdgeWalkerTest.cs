using System.Collections.Generic;
using System.Linq;
using C7Engine.Pathing;
using C7GameData;
using C7GameData.Save;
using EngineTests.Utils;
using Xunit;

namespace EngineTests.AI.Pathing {
	public sealed class WalkerOnLandTest : MapBase {
		[Fact]
		private void TestHumanPlayerLandUnitIgnoresKnownWater() {
			InitilizeStartTile(MakeHillTile(), new TileLocation(50, 50));

			var coast = AddNeighborsAndUpdateMap(startTile, MakeCoastTile(), TileDirection.NORTH);
			var mountain = AddNeighborsAndUpdateMap(startTile, MakeMountainTile(), TileDirection.SOUTH);
			var plains = AddNeighborsAndUpdateMap(startTile, MakePlainsTile(), TileDirection.WEST);

			float movementPoints = 2.0f;
			MapUnit unit =  MakeLandUnit((int)movementPoints);

			// make player human, so that they can't see unknown tiles
			unit.owner.isHuman = true;

			// Add tiles to Player's known tiles
			unit.owner.tileKnowledge.knownTiles.Add(startTile);
			unit.owner.tileKnowledge.knownTiles.Add(coast);
			unit.owner.tileKnowledge.knownTiles.Add(mountain);
			unit.owner.tileKnowledge.knownTiles.Add(plains);

			UnitWalker unitWalker = new(unit);

			// The water tile should be ignored, and the costs should be correct.
			IEnumerable<Edge<Tile>> edges = unitWalker.getEdges(startTile);
			Assert.Equal(2, edges.Count());

			Assert.Contains(edges, item => item.current == mountain && item.distanceToCurrent == 1);
			Assert.Contains(edges, item => item.current == plains && item.distanceToCurrent == 1 / movementPoints);
		}

		[Fact]
		private void TestHumanPlayerLandUnitIncludesUnknownWater() {
			InitilizeStartTile(MakeHillTile(), new TileLocation(50, 50));

			var coast = AddNeighborsAndUpdateMap(startTile, MakeCoastTile(), TileDirection.NORTH);
			var mountain = AddNeighborsAndUpdateMap(startTile, MakeMountainTile(), TileDirection.SOUTH);
			var plains = AddNeighborsAndUpdateMap(startTile, MakePlainsTile(), TileDirection.WEST);

			float movementPoints = 2.0f;
			MapUnit unit =  MakeLandUnit((int)movementPoints);
			unit.owner.isHuman = true;

			// Add tiles to Player's known tiles
			unit.owner.tileKnowledge.knownTiles.Add(startTile);
			unit.owner.tileKnowledge.knownTiles.Add(mountain);
			unit.owner.tileKnowledge.knownTiles.Add(plains);

			UnitWalker unitWalker = new(unit);

			// The water tile should not be ignored, and the costs should be correct.
			IEnumerable<Edge<Tile>> edges = unitWalker.getEdges(startTile);
			Assert.Equal(3, edges.Count());

			Assert.Contains(edges, item => item.current == mountain && item.distanceToCurrent == 1);
			Assert.Contains(edges, item => item.current == plains && item.distanceToCurrent == 1 / movementPoints);
			// 1 cost because the human player does not know the nature of the unexplored tile
			Assert.Contains(edges, item => item.current == coast && item.distanceToCurrent == 1 / movementPoints);
		}

		[Fact]
		private void TestAiPlayerLandUnitIgnoresUnknownWater() {
			InitilizeStartTile(MakeHillTile(), new TileLocation(50, 50));

			var coast = AddNeighborsAndUpdateMap(startTile, MakeCoastTile(), TileDirection.NORTH);
			var mountain = AddNeighborsAndUpdateMap(startTile, MakeMountainTile(), TileDirection.SOUTH);
			var plains = AddNeighborsAndUpdateMap(startTile, MakePlainsTile(), TileDirection.WEST);

			float movementPoints = 2.0f;
			MapUnit unit =  MakeLandUnit((int)movementPoints);

			// make player human, so that they can't see unknown tiles
			unit.owner.isHuman = false;

			UnitWalker unitWalker = new(unit);

			// The water tile should be ignored, even if it's unexplored because player is AI, and the costs should be correct.
			IEnumerable<Edge<Tile>> edges = unitWalker.getEdges(startTile);
			Assert.Equal(2, edges.Count());

			Assert.Contains(edges, item => item.current == mountain && item.distanceToCurrent == 1);
			Assert.Contains(edges, item => item.current == plains && item.distanceToCurrent == 1 / movementPoints);
		}

		[Fact]
		private void TestRoadOnDestinationNotOnStart() {
			InitilizeStartTile(MakeHillTile(), new TileLocation(50, 50));

			// Set up a neighbor with a road.
			var plains = AddNeighborsAndUpdateMap(startTile, MakePlainsTile(), TileDirection.NORTH);
			plains.overlays.Add(road);

			float movementPoints = 2.0f;
			MapUnit unit =  MakeLandUnit((int)movementPoints);

			UnitWalker unitWalker = new(unit);

			// The road shouldn't matter, since we don't have a road.
			IEnumerable<Edge<Tile>> edges = unitWalker.getEdges(startTile);
			Assert.Single(edges);
			Assert.Contains(edges, item => item.current == plains && item.distanceToCurrent == 1 / 2.0f);
		}

		[Fact]
		private void TestRoadOnStartNotOnDestination() {
			InitilizeStartTile(MakeHillTile(), new TileLocation(50, 50));
			startTile.overlays.Add(road);

			// Set up a neighbor without a road.
			var plains = AddNeighborsAndUpdateMap(startTile, MakePlainsTile(), TileDirection.NORTH);

			float movementPoints = 2.0f;
			MapUnit unit =  MakeLandUnit((int)movementPoints);

			UnitWalker unitWalker = new(unit);

			// The road shouldn't matter, since the destination doesn't have a road.
			IEnumerable<Edge<Tile>> edges = unitWalker.getEdges(startTile);
			Assert.Single(edges);
			Assert.Contains(edges, item => item.current == plains && item.distanceToCurrent == 1 / 2.0f);
		}

		[Fact]
		private void TestRoadOnStartAndDestination() {
			InitilizeStartTile(MakeHillTile(), new TileLocation(50, 50));
			startTile.overlays.Add(road);

			// Set up a neighbor without a road.
			var plains = AddNeighborsAndUpdateMap(startTile, MakePlainsTile(), TileDirection.NORTH);
			plains.overlays.Add(road);

			float movementPoints = 2.0f;
			MapUnit unit =  MakeLandUnit((int)movementPoints);

			UnitWalker unitWalker = new(unit);

			// The cost should be adjusted because we both have a road.
			IEnumerable<Edge<Tile>> edges = unitWalker.getEdges(startTile);
			Assert.Single(edges);
			Assert.Contains(edges, item => item.current == plains && item.distanceToCurrent == 1.0f / 3.0f / 2.0f);
		}
	}

	public sealed class WalkerOnWaterTest : MapBase {
		[Fact]
		private void TestHumanWaterUnitIgnoresKnownLand() {
			InitilizeStartTile(MakeCoastTile(), new TileLocation(50, 50));

			// Add 2 neighbors, one of which is land.
			var hills = AddNeighborsAndUpdateMap(startTile, MakeHillTile(), TileDirection.NORTH);
			var sea = AddNeighborsAndUpdateMap(startTile, MakeSeaTile(), TileDirection.SOUTH);

			float movementPoints = 2.0f;
			MapUnit unit =  MakeWaterUnit((int)movementPoints);

			// make player human, so that they can't see unknown tiles
			unit.owner.isHuman = true;

			// Add tiles to Player's known tiles
			unit.owner.tileKnowledge.knownTiles.Add(sea);
			unit.owner.tileKnowledge.knownTiles.Add(hills);
			unit.owner.tileKnowledge.knownTiles.Add(sea);

			UnitWalker unitWalker = new(unit);

			// The land tile should be ignored, and the costs should be correct.
			IEnumerable<Edge<Tile>> edges = unitWalker.getEdges(startTile);
			Assert.Single(edges);

			Assert.Contains(edges, item => item.current == sea && item.distanceToCurrent == 1 / movementPoints);
		}

		[Fact]
		private void TestHumanWaterUnitDoesNotIgnoreUnknownLand() {
			InitilizeStartTile(MakeCoastTile(), new TileLocation(50, 50));

			// Add 2 neighbors, one of which is land.
			var hill = AddNeighborsAndUpdateMap(startTile, MakeHillTile(), TileDirection.NORTH);
			var sea = AddNeighborsAndUpdateMap(startTile, MakeSeaTile(), TileDirection.SOUTH);

			float movementPoints = 2.0f;
			MapUnit unit =  MakeWaterUnit((int)movementPoints);

			// make player human, so that they can't see unknown tiles
			unit.owner.isHuman = true;

			// Add tiles to Player's known tiles
			unit.owner.tileKnowledge.knownTiles.Add(startTile);
			unit.owner.tileKnowledge.knownTiles.Add(sea);

			UnitWalker unitWalker = new(unit);

			// The land tile should be ignored, and the costs should be correct.
			IEnumerable<Edge<Tile>> edges = unitWalker.getEdges(startTile);
			Assert.Equal(2, edges.Count());

			Assert.Contains(edges, item => item.current == sea && item.distanceToCurrent == 1 / movementPoints);
			Assert.Contains(edges, item => item.current == hill && item.distanceToCurrent == 1 / movementPoints);
		}

		[Fact]
		private void TestLandIncludedIfItHasCityWithSameOwner() {
			InitilizeStartTile(MakeCoastTile(), new TileLocation(50, 50));
			var hill = AddNeighborsAndUpdateMap(startTile, MakeHillTile(), TileDirection.NORTH);

			float movementPoints = 2.0f;
			MapUnit unit =  MakeWaterUnit((int)movementPoints);

			// Set up a neighbor on land with a city of the same owner.
			hill.cityAtTile = new City(Tile.NONE, unit.owner, "", ID.None(""));

			// Add tiles to Player's known tiles
			unit.owner.tileKnowledge.knownTiles.Add(startTile);
			unit.owner.tileKnowledge.knownTiles.Add(hill);

			UnitWalker walker = new(unit);

			// The city tile should be included, to allow for canals, and so
			// that ships can go back into harbors.
			//
			// The cost should be 1, despite the city being on a hill. Land
			// movement costs don't make sense to apply to ships.
			IEnumerable<Edge<Tile>> edges = walker.getEdges(startTile);
			Assert.Single(edges);
			Assert.Contains(edges, item => item.current == hill && item.distanceToCurrent == 1 / movementPoints);
		}

		[Fact]
		private void TestCityWithDifferentOwnerNotIncluded() {
			InitilizeStartTile(MakeCoastTile(), new TileLocation(50, 50));
			var hill = AddNeighborsAndUpdateMap(startTile, MakeHillTile(), TileDirection.NORTH);

			float movementPoints = 2.0f;
			MapUnit unit =  MakeWaterUnit((int)movementPoints);

			Player otherPlayer = new Player();

			// Set up a neighbor on land with a city of the same owner.
			Tile end = hill;
			end.cityAtTile = new City(Tile.NONE, otherPlayer, "", ID.None(""));

			// Add tiles to Player's known tiles
			unit.owner.tileKnowledge.knownTiles.Add(startTile);
			unit.owner.tileKnowledge.knownTiles.Add(end);

			UnitWalker walker = new(unit);

			// The city tile should be included, to allow for canals, and so
			// that ships can go back into harbors.
			//
			// The cost should be 1, despite the city being on a hill. Land
			// movement costs don't make sense to apply to ships.
			IEnumerable<Edge<Tile>> edges = walker.getEdges(startTile);
			Assert.Empty(edges);
		}
	}
}
