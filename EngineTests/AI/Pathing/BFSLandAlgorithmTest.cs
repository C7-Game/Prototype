using System;
using System.Collections.Generic;
using C7Engine.Pathing;
using C7GameData;
using Xunit;

namespace EngineTests
{
	public class BFSLandAlgorithmTest
	{
		[Fact]
		public void ConstructPath_CreatesASamplePathProperly()
		{
			Dictionary<Tile, Tile> predecessors = new Dictionary<Tile, Tile>();
			Tile start = new Tile { xCoordinate = 34, yCoordinate = 18 };
			Tile tileTwo = new Tile { xCoordinate = 34, yCoordinate = 20 };
			Tile tileThree = new Tile { xCoordinate = 34, yCoordinate = 22 };
			Tile tileFour = new Tile { xCoordinate = 33, yCoordinate = 23 };
			Tile tileFive = new Tile { xCoordinate = 33, yCoordinate = 25 };
			Tile destination = new Tile { xCoordinate = 35, yCoordinate = 25 };
			predecessors[destination] = tileFive;
			predecessors[tileFive] = tileFour;
			predecessors[tileFour] = tileThree;
			predecessors[tileThree] = tileTwo;
			predecessors[tileTwo] = start;
			TilePath path = new BFSLandAlgorithm().ConstructPath(destination, predecessors);

			Assert.Equal(tileTwo, path.Next());
			Assert.Equal(tileThree, path.Next());
			Assert.Equal(tileFour, path.Next());
			Assert.Equal(tileFive, path.Next());
			Assert.Equal(destination, path.Next());
		}
	}
}
