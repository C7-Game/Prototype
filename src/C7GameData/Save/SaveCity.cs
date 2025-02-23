using System;
using System.Collections.Generic;

namespace C7GameData.Save {
	public class SaveCityResident {
		public ID citizenType;
		public string nationality;
		public ID city;
		public TileLocation tileWorked;
	}

	public enum ProducibleType { WEALTH, BUILDING, UNIT };

	public class SaveCity : IHasID {
		public ID id { get; set; }
		public ID owner;
		public bool capital;
		public TileLocation location;
		public string producible;
		public ProducibleType producibleType;
		public string name;
		public int size;
		public Dictionary<string, int> perPlayerCulture = new();
		public int shieldsStored;
		public int foodStored;
		public int foodNeededToGrow;
		public List<SaveCityResident> residents = new List<SaveCityResident>();

		public SaveCity() { }

		public SaveCity(City city) {
			id = city.id;
			owner = city.owner.id;
			capital = city.capital;
			location = new TileLocation(city.location);
			name = city.name;
			size = city.size;
			producible = city.itemBeingProduced.name;
			producibleType = city.itemBeingProduced switch {
				UnitPrototype => ProducibleType.UNIT,
				Building => ProducibleType.BUILDING,
			};
			shieldsStored = city.shieldsStored;
			foodStored = city.foodStored;
			foodNeededToGrow = city.foodNeededToGrow;
			residents = city.residents.ConvertAll(resident => {
				return new SaveCityResident {
					nationality = resident.nationality?.name,
					city = resident.city.id,
					tileWorked = new TileLocation(resident.tileWorked),
				};
			});
			foreach (KeyValuePair<Player, int> keyValuePair in city.perPlayerCulture) {
				perPlayerCulture.Add(keyValuePair.Key.id.ToString(), keyValuePair.Value);
			}
		}

		public City ToCity(GameMap gameMap,
							List<Player> players,
							List<UnitPrototype> unitPrototypes,
							List<Civilization> civilizations,
							List<Building> buildings,
							List<CitizenType> citizenTypes,
							Action<City, CitizenType> assignScenarioResidents) {
			City city = new City{
				id = id,
				location = gameMap.tileAt(location.X, location.Y),
				owner = players.Find(p => p.id == owner),
				name = name,
				size = size,
				itemBeingProduced = producibleType switch {
					ProducibleType.UNIT => unitPrototypes.Find(proto => proto.name == producible),
					ProducibleType.BUILDING => buildings.Find(building => building.name == producible),
				},
				shieldsStored = shieldsStored,
				foodStored = foodStored,
				foodNeededToGrow = foodNeededToGrow,
				capital = capital,
			};

			foreach (KeyValuePair<string, int> keyValuePair in perPlayerCulture) {
				city.perPlayerCulture.Add(players.Find(x => x.id.ToString() == keyValuePair.Key), keyValuePair.Value);
			}

			city.residents = residents.ConvertAll(resident => {
				return new CityResident {
					citizenType = citizenTypes.Find(x => x.Id == resident.citizenType),
					nationality = civilizations.Find(civ => civ.name == resident.nationality),
					tileWorked = gameMap.tileAt(resident.tileWorked.X, resident.tileWorked.Y),
					city = city,
				};
			});

			// Fill in the back pointers.
			foreach (CityResident cr in city.residents) {
				cr.tileWorked.personWorkingTile = cr;
			}

			// Scenarios don't specify the citizens of each city, only the city
			// size. So we need to do that assignment now. This requires using
			// the tile assignment AI, which due to dependency reasons we can't
			// access directly, so we do this via a lambda.
			if (city.residents.Count == 0) {
				assignScenarioResidents(city, citizenTypes.Find(x => x.IsDefaultCitizen));
			}

			return city;
		}
	}
}
