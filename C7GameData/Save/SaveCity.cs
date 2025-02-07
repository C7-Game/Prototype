using System.Collections.Generic;

namespace C7GameData.Save {
	public class SaveCityResident {
		public string nationality;
		public TileLocation tileWorked;
	}

	public class SaveCity : IHasID {
		public ID id { get; set; }
		public ID owner;
		public bool capital;
		public TileLocation location;
		public string producible;
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
			shieldsStored = city.shieldsStored;
			foodStored = city.foodStored;
			foodNeededToGrow = city.foodNeededToGrow;
			residents = city.residents.ConvertAll(resident => {
				return new SaveCityResident {
					nationality = resident.nationality?.name,
					tileWorked = new TileLocation(resident.tileWorked),
				};
			});
		}

		public City ToCity(GameMap gameMap, List<Player> players, List<UnitPrototype> unitPrototypes, List<Civilization> civilizations) {
			City city = new City{
				id = id,
				location = gameMap.tileAt(location.X, location.Y),
				owner = players.Find(p => p.id == owner),
				name = name,
				size = size,
				itemBeingProduced = unitPrototypes.Find(proto => proto.name == producible),
				shieldsStored = shieldsStored,
				foodStored = foodStored,
				foodNeededToGrow = foodNeededToGrow,
				capital = capital,
				residents = residents.ConvertAll(resident =>{
					return new CityResident{
						tileWorked = gameMap.tileAt(resident.tileWorked.X, resident.tileWorked.Y),
						nationality = civilizations.Find(civ => civ.name == resident.nationality),
					};
				}),
			};
			return city;
		}
	}
}
