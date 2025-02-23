using System.Linq;
using C7Engine.AI;

namespace C7Engine {
	using System;
	using C7GameData;

	public class CityInteractions {
		public static City BuildCity(int X, int Y, ID playerID, string name) {
			GameData gameData = EngineStorage.gameData;
			Player owner = gameData.GetPlayer(playerID);
			Tile tileWithNewCity = gameData.map.tileAt(X, Y);
			City newCity = new City(tileWithNewCity, owner, name, gameData.ids.CreateID("city"));
			if (owner.cities.Count == 0) {
				newCity.capital = true;
			}
			gameData.cities.Add(newCity);
			owner.cities.Add(newCity);
			tileWithNewCity.cityAtTile = newCity;

			// Update owners before we assign the citizen so the tile owners are
			// accurate.
			gameData.UpdateTileOwners();

			CityResident firstResident = new CityResident();
			firstResident.city = newCity;
			firstResident.citizenType = gameData.citizenTypes.Find(x => x.IsDefaultCitizen);
			CityTileAssignmentAI.AssignNewCitizenToTile(firstResident);
			newCity.SetItemBeingProduced(CityProductionAI.GetNextItemToBeProduced(newCity, null));

			// Cities are treated as though they have a road, but if
			// a city is build on a mine, the mine should be removed.
			tileWithNewCity.overlays.road = true;
			tileWithNewCity.overlays.mine = false;
			tileWithNewCity.overlays.irrigation = false;

			return newCity;
		}

		public static void DestroyCity(int X, int Y) {
			Tile tile = EngineStorage.gameData.map.tileAt(X, Y);
			tile.DisbandNonDefendingUnits();
			tile.cityAtTile.RemoveAllCitizens();
			tile.cityAtTile.owner.cities.Remove(tile.cityAtTile);
			EngineStorage.gameData.cities.Remove(tile.cityAtTile);
			EngineStorage.gameData.UpdateTileOwnersOnCityDestruction(tile.cityAtTile);
			new MsgCityDestroyed(tile.cityAtTile).send();
			tile.cityAtTile = null;
			tile.overlays.road = false;
		}
	}
}
