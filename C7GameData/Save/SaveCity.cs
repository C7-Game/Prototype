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
		}

		public City ToCity(GameMap gameMap, List<Player> players, List<UnitPrototype> unitPrototypes, List<Civilization> civilizations, List<Building> buildings, List<CitizenType> citizenTypes) {
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
			return city;
		}
	}
}
