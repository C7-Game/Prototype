using System.Linq;
using C7Engine.AI;

namespace C7Engine
{
	using C7GameData;

	public class CityInteractions
	{
		public static void BuildCity(int x, int y, string playerGuid, string name)
		{
			GameData gameData = EngineStorage.gameData;
			Player owner = gameData.players.Find(player => player.guid == playerGuid);
			Tile tileWithNewCity = gameData.map.tileAt(x, y);
			City newCity = new City(tileWithNewCity, owner, name);
			CityResident firstResident = new CityResident();
			CityTileAssignmentAI.AssignNewCitizenToTile(newCity, firstResident);
			newCity.SetItemBeingProduced(CityProductionAI.GetNextItemToBeProduced(newCity, null));
			gameData.cities.Add(newCity);
			owner.cities.Add(newCity);
			tileWithNewCity.cityAtTile = newCity;
			tileWithNewCity.overlays.road = true;
		}
	}
}
