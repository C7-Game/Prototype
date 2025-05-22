using System;
using System.Collections.Generic;
using System.Linq;
using C7GameData.Save;
using C7Engine;

namespace C7GameData {
	public static class BuildingRules {
		public static Dictionary<SaveBuilding.Flag, Func<City, bool>> productionRules = new()
		{
			{ SaveBuilding.Flag.MustBeCoastal, MustBeCoastal },
			{ SaveBuilding.Flag.MustBeNearRiver, MustBeNearRiver }
		};

		public static Dictionary<SaveBuilding.Flag, Action<MapUnit>> unitProductionEffects = new()
		{
			{ SaveBuilding.Flag.VeteranGroundUnits, VeteranGroundUnits },
			{ SaveBuilding.Flag.VeteranSeaUnits, VeteranSeaUnits }
		};

		public static Dictionary<SaveBuilding.Flag, Action<Tile.Yield>> tileModifiers = new(){
			{ SaveBuilding.Flag.IncreasesFoodInWater, IncreaseFoodInWater },
			{ SaveBuilding.Flag.IncreasesShieldsInWater, IncreaseShieldInWater},
			{ SaveBuilding.Flag.IncreasesTradeInWater, IncreaseTradeInWater}
		};

		private static void IncreaseTradeInWater(Tile.Yield yield) {
			if (yield.type == Tile.YieldType.Commerce && yield.tile.IsWater() && yield.baseYield > 0) {
				yield.bonus += 1;
			}
		}

		private static void IncreaseShieldInWater(Tile.Yield yield) {
			if (yield.type == Tile.YieldType.Production && yield.tile.IsWater()) {
				yield.bonus += 1;
			}
		}

		private static void IncreaseFoodInWater(Tile.Yield yield) {
			if (yield.type == Tile.YieldType.Food && yield.tile.IsWater()) {
				yield.bonus += 1;
			}
		}

		private static void VeteranGroundUnits(MapUnit unit) {
			if (unit.unitType.categories.Contains("Land")) {
				unit.Promote();
			}
		}

		private static void VeteranSeaUnits(MapUnit unit) {
			if (unit.unitType.categories.Contains("Sea")) {
				unit.Promote();
			}
		}

		private static bool MustBeCoastal(City city) {
			return city.location.NeighborsWater();
		}

		private static bool MustBeNearRiver(City city) {
			return city.location.BordersRiver();
		}
	}

	public class Building : IProducible {
		public string name { get; set; }
		public int shieldCost { get; set; }
		public int populationCost { get; set; } // Will always be equal to 0 in the Civ3 rule set

		// Filled in in SaveGame::ConvertBuildings
		public Tech requiredTech { get; set; }
		public Tech? renderedObsoleteBy;

		public Building requiredBuilding;

		List<Func<City, bool>> productionPrerequisites = [];
		public Action<MapUnit> onFinishedUnitProduction;
		public Action<Tile.Yield> tileModifier;

		public bool isGreatWonder;
		public bool isSmallWonder;
		public bool isCenterOfEmpire;
		public bool increasesLuxuryTrade;
		public bool reducesCorruption;
		public bool isForbiddenPalace;
		public bool allowsCitySize2;
		public bool allowsCitySize3;
		public bool doublesCityGrowthRate;

		public int culturePerTurn = 0;

		// The number of unhappy faces that become content in the city with this
		// building.
		public int contentFacesInCity = 0;

		// The number of happy faces that become content in the city with this
		// building. Note that this is less powerful than other sources of
		// unhappiness, like drafting or poprushing, which converts happy faces
		// to sad faces.
		public int unhappyFacesInCity = 0;

		public HashSet<Resource> requiredResources { get; set; } = [];

		public int iconRowIndex = 0;

		SaveBuilding dataSource;

		public Building(SaveBuilding building) {
			name = building.name;
			shieldCost = building.shieldCost;
			populationCost = building.populationCost;
			isGreatWonder = building.isGreatWonder;
			isSmallWonder = building.isSmallWonder;
			culturePerTurn = building.culturePerTurn;
			iconRowIndex = building.iconRowIndex;
			if (building.contentFacesInCity < 0) {
				unhappyFacesInCity = -building.contentFacesInCity;
			} else {
				contentFacesInCity = building.contentFacesInCity;
			}
			isCenterOfEmpire = building.flags.Contains(SaveBuilding.Flag.IsCenterOfEmpire);
			increasesLuxuryTrade = building.flags.Contains(SaveBuilding.Flag.IncreasesLuxuryTrade);
			reducesCorruption = building.flags.Contains(SaveBuilding.Flag.ReducesCorruption);
			isForbiddenPalace = building.flags.Contains(SaveBuilding.Flag.ForbiddenPalace);
			allowsCitySize2 = building.flags.Contains(SaveBuilding.Flag.AllowsCitySize2);
			allowsCitySize3 = building.flags.Contains(SaveBuilding.Flag.AllowsCitySize3);
			doublesCityGrowthRate = building.flags.Contains(SaveBuilding.Flag.DoublesCityGrowthRate);
			dataSource = building;

			foreach (var kvp in BuildingRules.productionRules) {
				if (building.flags.Contains(kvp.Key)) {
					productionPrerequisites.Add(kvp.Value);
				}
			}

			foreach (var kvp in BuildingRules.unitProductionEffects) {
				if (building.flags.Contains(kvp.Key)) {
					onFinishedUnitProduction += kvp.Value;
				}
			}

			foreach (var kvp in BuildingRules.tileModifiers) {
				if (building.flags.Contains(kvp.Key)) {
					tileModifier += kvp.Value;
				}
			}
		}

		public bool CanProduce(City city, HashSet<Resource> accessibleResources) {
			if (!city.owner.HasRequiredTechnology(this)) {
				return false;
			}

			if (renderedObsoleteBy != null && city.owner.knownTechs.Contains(renderedObsoleteBy.id)) {
				return false;
			}

			if (city.buildings.Exists(cityBuilding => cityBuilding.building == this)) {
				return false;
			}

			if (isGreatWonder) {
				// We can't build a great wonder if it was already built.
				if (EngineStorage.gameData.GreatWondersBuilt.Contains(name)) {
					return false;
				}

				// We can't build a great wonder if another one of our cities is
				// building it.
				foreach (City c in city.owner.cities) {
					if (c.itemBeingProduced != null && c.itemBeingProduced.name == name) {
						return false;
					}
				}
			}

			// TODO: Add logic for wonders and the palace
			if (isSmallWonder || isCenterOfEmpire) {
				return false;
			}

			if (requiredBuilding != null &&
				!city.buildings.Exists(cityBuilding => cityBuilding.building == this)) {
				return false;
			}

			if (!requiredResources.All(accessibleResources.Contains)) {
				return false;
			}

			if (!productionPrerequisites.All(func => func(city))) {
				return false;
			}

			return true;
		}

		public void CreationEffects(City owningCity) {
			if (dataSource.buildingGainedInEveryCity != null) {
				Building b = EngineStorage.gameData.Buildings.Find(x => x.name == dataSource.buildingGainedInEveryCity);
				foreach (City c in owningCity.owner.cities) {
					c.AddBuilding(b, CityBuilding.Source.ProvidedByWonder);
				}
			}
			if (dataSource.buildingGainedInEveryCityOnContinent != null) {
				Building b = EngineStorage.gameData.Buildings.Find(x => x.name == dataSource.buildingGainedInEveryCityOnContinent);
				foreach (City c in owningCity.owner.cities) {
					if (c.location.continent != owningCity.location.continent) {
						continue;
					}

					c.AddBuilding(b, CityBuilding.Source.ProvidedByWonder);
				}
			}
		}

		public void EffectsOnNewCities(City owningCity, City newCity) {
			if (dataSource.buildingGainedInEveryCity != null) {
				Building b = EngineStorage.gameData.Buildings.Find(x => x.name == dataSource.buildingGainedInEveryCity);
				newCity.AddBuilding(b, CityBuilding.Source.ProvidedByWonder);
			}
			if (dataSource.buildingGainedInEveryCityOnContinent != null) {
				Building b = EngineStorage.gameData.Buildings.Find(x => x.name == dataSource.buildingGainedInEveryCityOnContinent);
				if (newCity.location.continent == owningCity.location.continent) {
					newCity.AddBuilding(b, CityBuilding.Source.ProvidedByWonder);
				}
			}
		}

		public void DestructionEffects(City previouslyOwningCity) {
			if (dataSource.buildingGainedInEveryCity != null) {
				Building b = EngineStorage.gameData.Buildings.Find(x => x.name == dataSource.buildingGainedInEveryCity);
				foreach (City c in previouslyOwningCity.owner.cities) {
					c.RemoveBuilding(b, CityBuilding.Source.ProvidedByWonder);
				}
			}
			if (dataSource.buildingGainedInEveryCityOnContinent != null) {
				Building b = EngineStorage.gameData.Buildings.Find(x => x.name == dataSource.buildingGainedInEveryCityOnContinent);
				foreach (City c in previouslyOwningCity.owner.cities) {
					if (c.location.continent != previouslyOwningCity.location.continent) {
						continue;
					}

					c.RemoveBuilding(b, CityBuilding.Source.ProvidedByWonder);
				}
			}
		}

		public SaveBuilding ToSaveBuilding() {
			return dataSource;
		}
	}
}
