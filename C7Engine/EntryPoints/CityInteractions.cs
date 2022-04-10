namespace C7Engine
{
	using System;
	using C7GameData;

	public class CityInteractions
	{
		public static void BuildCity(int x, int y, Guid playerGuid, string name)
		{
			GameData gameData = EngineStorage.gameData;
			Player owner = gameData.GetPlayer(playerGuid);
			Tile tileWithNewCity = gameData.map.tileAt(x, y);
			City newCity = new City(tileWithNewCity, owner, name);
			newCity.SetItemBeingProduced(gameData.unitPrototypes["Warrior"]);
			gameData.cities.Add(newCity);
			owner.cities.Add(newCity);
			tileWithNewCity.cityAtTile = newCity;
		}
	}
}
