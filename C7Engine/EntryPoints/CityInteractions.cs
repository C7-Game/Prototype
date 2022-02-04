using System;
using System.Collections.Generic;

namespace C7Engine
{
    using C7GameData;

    public class CityInteractions
    {
        //Eventually, this will need more info such as player
        public static void BuildCity(int x, int y, string playerGuid, string name)
        {
            GameData gameData = EngineStorage.gameData;

            Player owner = gameData.players.Find(player => player.guid == playerGuid);

			Tile tileWithNewCity = MapInteractions.GetTileAt(x, y);
			City newCity = new City(tileWithNewCity, owner, name, OnUnitCompleted, GetNextItemToBeProduced);
			newCity.SetItemBeingProduced(gameData.unitPrototypes["Warrior"]);
            gameData.cities.Add(newCity);

            tileWithNewCity.cityAtTile = newCity;
        }

		public static void OnUnitCompleted(MapUnit newUnit)
		{
				EngineStorage.gameData.mapUnits.Add(newUnit);
		}

		public static IProducable GetNextItemToBeProduced(City city, IProducable lastProduced)
		{
			Dictionary<string, UnitPrototype> unitPrototypes = EngineStorage.gameData.unitPrototypes;
			if (lastProduced == unitPrototypes["Warrior"]) {
				if (city.location.NeighborsCoast()) {
					Random rng = new Random();
					if (rng.Next(3) == 0) {
						return unitPrototypes["Galley"];
					}
					else {
						return unitPrototypes["Chariot"];
					}
				}
				else {
					return unitPrototypes["Chariot"];
				}
			}
			else if (lastProduced == unitPrototypes["Chariot"]) {
				return unitPrototypes["Settler"];
			}
			else  {
				return unitPrototypes["Warrior"];
			}
		}
    }
}