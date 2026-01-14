
using System.Collections.Generic;
using System.Linq;
using C7GameData;
using C7GameData.Save;
using EngineTests.Utils;
using Xunit;

namespace EngineTests.GameData;

public class GameMapTest : MapBase {
	[Fact]
	public void TestCorrectContinentCalculation() {
		// <~ ~ ~ > is sea
		// <______> is plains
		// <oooooo> is coast that is freshwater (lake)
		//
		//
		//           <~ ~ ~ ><~ ~ ~ ><~ ~ ~ ><~ ~ ~ ><~ ~ ~ >
		//       <~ ~ ~ ><~ ~ ~ ><~ ~ ~ ><~ ~ ~ ><~ ~ ~ >
		//           <~ ~ ~ ><~ ~ ~ ><~ ~ ~ ><~ ~ ~ ><~ ~ ~ >    <---------- sea might not be exact, but the point is there is a block of sea
		//       <~ ~ ~ ><~ ~ ~ ><~ ~ ~ ><~ ~ ~ ><~ ~ ~ >
		//                   <~ ~ ~ ><______>
		//                       <~ ~ ~ ><______>
		//                   <______><______><______>
		//               <______><oooooo><oooooo><______>         <---------- first tile on the left here is the starting tile
		//                   <______><oooooo><______>
		//                       <______><______>
		//                           <______>



		InitilizeStartTile(MakePlainsTile(), new TileLocation(50, 50));
		Tile x1y1 = AddNeighborsAndUpdateMap(startTile, MakePlainsTile(), TileDirection.SOUTHEAST);
		Tile x1y2 = AddNeighborsAndUpdateMap(x1y1, MakePlainsTile(), TileDirection.NORTH);

		Tile x2y1 = AddNeighborsAndUpdateMap(x1y1, MakePlainsTile(), TileDirection.SOUTHEAST);
		Tile x2y2 = AddNeighborsAndUpdateMap(x2y1, MakeCoastTile(), TileDirection.NORTH); // lake
		Tile x2y3 = AddNeighborsAndUpdateMap(x2y2, MakeCoastTile(), TileDirection.NORTH); // sea
		Tile x2y4 = AddNeighborsAndUpdateMap(x2y3, MakeCoastTile(), TileDirection.NORTHWEST); // sea

		// extra sea so x2y3 that we actually care to check against the lake
		// is not calculated as being freshwater, because it's less than 20 tiles
		Tile s01 = AddNeighborsAndUpdateMap(x2y3, MakeCoastTile(), TileDirection.NORTH); // sea
		Tile s02 = AddNeighborsAndUpdateMap(s01, MakeCoastTile(), TileDirection.EAST); // sea
		Tile s03 = AddNeighborsAndUpdateMap(s02, MakeCoastTile(), TileDirection.EAST); // sea
		Tile s04 = AddNeighborsAndUpdateMap(s01, MakeCoastTile(), TileDirection.WEST); // sea
		Tile s05 = AddNeighborsAndUpdateMap(s04, MakeCoastTile(), TileDirection.WEST); // sea

		Tile s06 = AddNeighborsAndUpdateMap(s03, MakeCoastTile(), TileDirection.NORTHEAST); // sea
		Tile s07 = AddNeighborsAndUpdateMap(s06, MakeCoastTile(), TileDirection.EAST); // sea
		Tile s08 = AddNeighborsAndUpdateMap(s07, MakeCoastTile(), TileDirection.EAST); // sea
		Tile s09 = AddNeighborsAndUpdateMap(s08, MakeCoastTile(), TileDirection.EAST); // sea
		Tile s10 = AddNeighborsAndUpdateMap(s09, MakeCoastTile(), TileDirection.EAST); // sea

		Tile s11 = AddNeighborsAndUpdateMap(s06, MakeCoastTile(), TileDirection.NORTHWEST); // sea
		Tile s12 = AddNeighborsAndUpdateMap(s11, MakeCoastTile(), TileDirection.EAST); // sea
		Tile s13 = AddNeighborsAndUpdateMap(s12, MakeCoastTile(), TileDirection.EAST); // sea
		Tile s14 = AddNeighborsAndUpdateMap(s13, MakeCoastTile(), TileDirection.EAST); // sea
		Tile s15 = AddNeighborsAndUpdateMap(s14, MakeCoastTile(), TileDirection.EAST); // sea

		Tile s16 = AddNeighborsAndUpdateMap(s11, MakeCoastTile(), TileDirection.NORTHEAST); // sea
		Tile s17 = AddNeighborsAndUpdateMap(s16, MakeCoastTile(), TileDirection.EAST); // sea
		Tile s18 = AddNeighborsAndUpdateMap(s17, MakeCoastTile(), TileDirection.EAST); // sea
		Tile s19 = AddNeighborsAndUpdateMap(s18, MakeCoastTile(), TileDirection.EAST); // sea
		Tile s20 = AddNeighborsAndUpdateMap(s19, MakeCoastTile(), TileDirection.EAST); // sea

		//

		Tile x3y1 = AddNeighborsAndUpdateMap(x2y1, MakePlainsTile(), TileDirection.SOUTHEAST);
		Tile x3y2 = AddNeighborsAndUpdateMap(x3y1, MakeCoastTile(), TileDirection.NORTH); // lake
		Tile x3y3 = AddNeighborsAndUpdateMap(x3y2, MakePlainsTile(), TileDirection.NORTH);
		Tile x3y4 = AddNeighborsAndUpdateMap(x3y3, MakePlainsTile(), TileDirection.NORTH);

		Tile x4y1 = AddNeighborsAndUpdateMap(x3y1, MakePlainsTile(), TileDirection.NORTHEAST);
		Tile x4y2 = AddNeighborsAndUpdateMap(x4y1, MakeCoastTile(), TileDirection.NORTH); // lake
		Tile x4y3 = AddNeighborsAndUpdateMap(x4y2, MakePlainsTile(), TileDirection.NORTH);

		Tile x5y1 = AddNeighborsAndUpdateMap(x4y1, MakePlainsTile(), TileDirection.NORTHEAST);
		Tile x5y2 = AddNeighborsAndUpdateMap(x5y1, MakePlainsTile(), TileDirection.NORTH);

		Tile x6y1 = AddNeighborsAndUpdateMap(x5y1, MakePlainsTile(), TileDirection.NORTHEAST);

		gameMap.tiles = new List<Tile>() {
			startTile,
			x1y1, x1y2,
			x2y1, x2y2, x2y3, x2y4,
			s01, s02, s03, s04, s05, s06, s07, s08, s09, s10, s11, s12, s13, s14, s15, s16, s17, s18, s19, s20,
			x3y1, x3y2, x3y3, x3y4,
			x4y1, x4y2, x4y3,
			x5y1, x5y2,
			x5y1, x5y2,
			x6y1
		};

		ComputeAllNeighbors(gameMap.tiles.ToHashSet());

		gameMap.recomputeContinents();

		// land is the same continent
		Assert.Equal(x1y1.continent, x2y1.continent);
		// land and water are not
		Assert.NotEqual(x1y1.continent, x2y2.continent);
		// sea and lake are not
		Assert.NotEqual(x2y2.continent, x2y3.continent);
		// all lake tiles are part of the same continent
		Assert.Equal(x2y2.continent, x3y2.continent);
		Assert.Equal(x2y2.continent, x4y2.continent);
		// lake is fresh water
		Assert.True(x2y2.isFreshWater);
		Assert.True(x3y2.isFreshWater);
		Assert.True(x4y2.isFreshWater);
		// random sea tiles are the same continent
		Assert.Equal(x2y3.continent, s01.continent);
		Assert.Equal(s01.continent, s04.continent);
		Assert.Equal(s04.continent, s08.continent);
		Assert.Equal(s08.continent, s13.continent);
		Assert.Equal(s13.continent, s19.continent);
		// random sea tiles are not fresh water
		Assert.False(x2y3.isFreshWater);
		Assert.False(s01.isFreshWater);
		Assert.False(s05.isFreshWater);
		Assert.False(s10.isFreshWater);
		Assert.False(s15.isFreshWater);
		Assert.False(s20.isFreshWater);
	}
}
