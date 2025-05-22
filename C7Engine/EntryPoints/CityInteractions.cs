using System.Linq;
using C7Engine.AI;

namespace C7Engine {
	using System;
	using C7GameData;

	public class CityInteractions {
		public static City BuildCity(Tile tileWithNewCity, Player owner, string name) {
			GameData gameData = EngineStorage.gameData;
			City newCity = new City(tileWithNewCity, owner, name, gameData.ids.CreateID("city"));
			if (owner.cities.Count == 0) {
				newCity.capital = true;
				newCity.AddBuilding(gameData.Buildings.Find(x => x.isCenterOfEmpire), CityBuilding.Source.Built);
			}

			// Apply any wonder effects to the new city (like adding a granary
			// if we already have the pyramids).
			foreach (City c in owner.cities) {
				foreach (CityBuilding cb in c.buildings) {
					cb.building.EffectsOnNewCities(c, newCity);
				}
			}

			gameData.cities.Add(newCity);
			owner.cities.Add(newCity);
			tileWithNewCity.cityAtTile = newCity;

			CityResident firstResident = new CityResident();
			firstResident.city = newCity;
			firstResident.citizenType = gameData.citizenTypes.Find(x => x.IsDefaultCitizen);
			newCity.AddCitizen(firstResident);

			// Update owners before we assign the citizen so the tile owners are
			// accurate. We do this after adding the resident though, because
			// cities with zero residents are considered destroyed.
			gameData.UpdateTileOwners();
			CityTileAssignmentAI.AssignNewCitizenToTile(firstResident);

			newCity.SetItemBeingProduced(CityProductionAI.GetNextItemToBeProduced(newCity, null));

			// Redo corruption calculations after a city is created, since it
			// may change rank corruption values.
			owner.DoCorruptionCalculations(EngineStorage.gameData);

			return newCity;
		}

		public static void DestroyCity(int X, int Y) {
			Tile tile = EngineStorage.gameData.map.tileAt(X, Y);
			tile.DisbandNonDefendingUnits();
			Player owner = tile.cityAtTile.owner;

			// If this city had a wonder that was providing buildings in other
			// cities, ensure everything gets updated properly.
			foreach (CityBuilding cb in tile.cityAtTile.buildings) {
				cb.building.DestructionEffects(tile.cityAtTile);
			}

			tile.cityAtTile.RemoveAllCitizens();
			tile.cityAtTile.owner.cities.Remove(tile.cityAtTile);
			EngineStorage.gameData.cities.Remove(tile.cityAtTile);
			EngineStorage.gameData.UpdateTileOwnersOnCityDestruction(tile.cityAtTile);
			new MsgCityDestroyed(tile.cityAtTile).send();
			if (EngineStorage.gameData.CheckForCivDestruction(tile.cityAtTile.owner)) {
				// Let the UI know about the civ destruction.
				new MsgCivilizationDestroyed(tile.cityAtTile.owner.civilization).send();
			}
			tile.cityAtTile = null;

			// Redo corruption calculations after a city is destroyed, since it
			// may change rank corruption values.
			owner.DoCorruptionCalculations(EngineStorage.gameData);
		}
	}
}
