using System.Linq;
using C7Engine.AI;

namespace C7Engine
{
	using C7GameData;

	public class CityInteractions
	{
		public static void BuildCity(int x, int y, ID playerID, string name)
		{
			GameData gameData = EngineStorage.gameData;
			Player owner = gameData.GetPlayer(playerID);
			Tile tileWithNewCity = gameData.map.tileAt(x, y);
			City newCity = new City(tileWithNewCity, owner, name, gameData.ids.CreateID("city"));
			CityResident firstResident = new CityResident();
			CityTileAssignmentAI.AssignNewCitizenToTile(newCity, firstResident);
			newCity.SetItemBeingProduced(CityProductionAI.GetNextItemToBeProduced(newCity, null));
			if (owner.cities.Count == 0) {
				newCity.capital = true;
			}
			gameData.cities.Add(newCity);
			owner.cities.Add(newCity);
			tileWithNewCity.cityAtTile = newCity;
			tileWithNewCity.overlays.road = true;
		}

		public static void DestroyCity(int x, int y) {
			Tile tile = EngineStorage.gameData.map.tileAt(x, y);
			tile.DisbandNonDefendingUnits();
			tile.cityAtTile.RemoveAllCitizens();
			tile.cityAtTile.owner.cities.Remove(tile.cityAtTile);
			EngineStorage.gameData.cities.Remove(tile.cityAtTile);
			tile.cityAtTile = null;
		}
	}
}
