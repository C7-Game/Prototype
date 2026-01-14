using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using C7Engine;

namespace C7GameData.Save {

	public static class TypeInfoResolver {
		public static void IgnoreDefaultValues(JsonTypeInfo jsonTypeInfo) {
			foreach (JsonPropertyInfo pi in jsonTypeInfo.Properties) {
				if (pi.PropertyType == typeof(string)) {
					pi.ShouldSerialize = (_, value) => ((string)value)?.Length > 0;
				} else if (typeof(ICollection).IsAssignableFrom(pi.PropertyType)) {
					pi.ShouldSerialize = (_, value) => ((ICollection)value)?.Count > 0;
				} else if (typeof(IEnumerable).IsAssignableFrom(pi.PropertyType)) {
					pi.ShouldSerialize = (_, value) => value is not null && ((IEnumerable)value).GetEnumerator().MoveNext();
				} else {
					pi.ShouldSerialize = (_, value) => value is not null;
				}
			}
		}
	}

	public class SaveGame {

		private static JsonSerializerOptions JsonOptions {
			get => new JsonSerializerOptions {
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				// Pretty print during development; may change this for production
				WriteIndented = true,
				// By default it only serializes getters, this makes it serialize fields, too
				IncludeFields = true,
				Converters = {
					new Json2DArrayConverter(),
					new IDJsonConverter(),
					new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
				},
				TypeInfoResolver = new DefaultJsonTypeInfoResolver {
					Modifiers = { TypeInfoResolver.IgnoreDefaultValues },
				},
			};
		}

		public SaveGame() { }

		public static SaveGame FromGameData(GameData data) {
			SaveGame save = new SaveGame {
				Seed = data.seed,
				TurnNumber = data.turn,
				Civilizations = data.civilizations,
				Map = new SaveMap(data.map),
				TerrainTypes = data.terrainTypes,
				Resources = data.Resources,
				Buildings = data.Buildings.ConvertAll(building => building.ToSaveBuilding()),
				GreatWondersBuilt = data.GreatWondersBuilt,
				BarbarianInfo = data.barbarianInfo,
				Units = data.mapUnits.ConvertAll(unit => new SaveUnit(unit, data.map)),
				UnitPrototypes = data.unitPrototypes.ConvertAll(proto => new SaveUnitPrototype(proto)),
				Players = data.players.ConvertAll(player => new SavePlayer(player)),
				Cities = data.cities.ConvertAll(city => new SaveCity(city)),
				ExperienceLevels = data.experienceLevels,
				ScenarioSearchPath = data.scenarioSearchPath,
				DefaultExperienceLevel = data.defaultExperienceLevelKey,
				Techs = data.techs.ConvertAll(t => t.ToSaveTech()),
				CitizenTypes = data.citizenTypes,
				TerraForms = data.Terraforms.ConvertAll(t => t.ToSaveTerraform()),
				Governments = data.governments,
				Difficulties = data.difficulties,
				GameDifficulty = data.gameDifficulty,
				Rules = data.rules,
				TerrainImprovements = data.terrainImprovements.ConvertAll(ti => ti.ToSaveTerrainImprovement())
			};
			save.StrengthBonuses.Add(data.fortificationBonus);
			save.StrengthBonuses.Add(data.riverCrossingBonus);
			save.StrengthBonuses.Add(data.cityLevel1DefenseBonus);
			save.StrengthBonuses.Add(data.cityLevel2DefenseBonus);
			save.StrengthBonuses.Add(data.cityLevel3DefenseBonus);
			save.HealRates["friendly_field"] = data.healRateInFriendlyField;
			save.HealRates["neutral_field"] = data.healRateInNeutralField;
			save.HealRates["hostile_field"] = data.healRateInHostileField;
			save.HealRates["city"] = data.healRateInCity;

			return save;
		}

		private void populateGameDataTileUnitsAndCities(GameData data) {
			foreach (Tile tile in data.map.tiles) {
				tile.unitsOnTile = data.mapUnits.Where(unit => unit.location == tile).ToList();
				tile.cityAtTile = data.cities.Find(city => city.location == tile);
			}
		}

		public GameData ToGameData(string luaRulesDir) {
			GameData data = InitializeGameData();

			// TODO: In the future the path to the Lua script should be loaded from a save
			// to allow a modded game to rely on a specific Lua ruleset.
			string rulesScript = "civ3.lua";
			data.luaRulesEngine.Initialize(luaRulesDir, rulesScript);

			ConvertTerrainImprovements(data);
			ConvertTerraforms(data);
			// convert technologies earlier than the player data,
			// because we need to fill in current research and research queues
			ConvertTechnologies(data);
			ConvertMapAndPlayers(data);
			ConvertBuildings(data);
			ConvertUnits(data);
			ConvertCities(data);
			ConvertBarbarianInfo(data);
			ConvertStrengthBonuses(data);
			ConvertHealRates(data);

			data.defaultExperienceLevelKey = DefaultExperienceLevel;
			data.defaultExperienceLevel = data.experienceLevels.Find(el => el.key == DefaultExperienceLevel);

			data.UpdateTileOwners();
			data.InvalidateCachedTradeNetwork();

			data.onGameCreation += OnGameCreation;

			return data;
		}

		private void OnGameCreation() {
			foreach (Player p in EngineStorage.gameData.players) {
				// Backfill citizen assignments for scenarios that don't specify
				// them.
				foreach (City c in p.cities) {
					foreach (CityResident cr in c.residents) {
						if (cr.citizenType.IsDefaultCitizen && cr.tileWorked == Tile.NONE) {
							C7Engine.AI.CityTileAssignmentAI.AssignNewCitizenToTile(EngineStorage.gameData, cr);
						}
					}
				}

				// Backfill visibility.
				foreach (MapUnit u in p.units) {
					p.tileKnowledge.AddTilesToKnown(u.location);
				}

				// TODO: this may require more than one loop, because if all the
				// citizens are happy it reduces wasted shields via we love the
				// king day.
				p.DoCorruptionCalculations(EngineStorage.gameData);
				p.RecalculateCitizenMoods(EngineStorage.gameData);
			}
		}

		private GameData InitializeGameData() {
			// copy data without references
			return new GameData {
				seed = Seed,
				turn = TurnNumber,
				terrainTypes = TerrainTypes,
				Resources = Resources,
				scenarioSearchPath = ScenarioSearchPath,
				civilizations = Civilizations,
				citizenTypes = CitizenTypes,
				governments = Governments,
				difficulties = Difficulties,
				gameDifficulty = GameDifficulty,
				ids = new ID.Factory(this),
				experienceLevels = ExperienceLevels,
				rules = Rules,
				GreatWondersBuilt = GreatWondersBuilt,
			};
		}

		private void ConvertTerraforms(GameData data) {
			data.Terraforms = TerraForms.ConvertAll(st => new Terraform(st, data));
		}

		private void ConvertMapAndPlayers(GameData data) {
			// units and cities are empty
			data.map = Map.ToGameMap(data);

			// players need game map to populate tile knowledge
			data.players = Players.ConvertAll(player => player.ToPlayer(data.map, Civilizations, data.governments, data.techs, data.rules));
		}

		private void ConvertTerrainImprovements(GameData gameData) {
			var saveByKey = TerrainImprovements.ToDictionary(s => s.key);
			var terrainTypeByKey = TerrainTypes.ToDictionary(tt => tt.Key);

			var created = new Dictionary<string, TerrainImprovement>();

			TerrainType ResolveTerrainType(string key) {
				return terrainTypeByKey[key];
			}

			TerrainImprovement Create(string key) {
				if (created.TryGetValue(key, out var existing)) {
					return existing;
				}

				var save = saveByKey[key];
				TerrainImprovement upgradesFrom = null;

				if (!string.IsNullOrEmpty(save.upgradesFrom)) {
					upgradesFrom = Create(save.upgradesFrom);
				}

				var improvement = new TerrainImprovement(save, gameData.luaRulesEngine, ResolveTerrainType, upgradesFrom);
				created[key] = improvement;
				return improvement;
			}

			foreach (var key in saveByKey.Keys) {
				Create(key);
			}

			gameData.terrainImprovements = created.Values.ToList();
		}

		private void ConvertTechnologies(GameData data) {
			// Fill in the list of techs and then backfill the prereqs.
			//
			// This is an N^2 approach, but doing a topological sort of the
			// prereqs feels like overkill given how many techs are in a game.

			foreach (SaveTech st in this.Techs) {
				data.techs.Add(st.ToTechWithoutPrereqs());
			}

			foreach (Tech t in data.techs) {
				t.FillInPrereqs(this.Techs, data.techs);
			}
		}

		private void ConvertBuildings(GameData data) {
			data.Buildings = Buildings.ConvertAll(building => new Building(building, data));

			var buildingDict = data.Buildings.ToDictionary(b => b.name);
			var techDict = data.techs.ToDictionary(t => t.id);
			var resDict = data.Resources.ToDictionary(r => r.Key);

			foreach (SaveBuilding saveBuilding in Buildings) {
				Building building = buildingDict[saveBuilding.name];

				if (saveBuilding.requiredBuilding != null) {
					building.requiredBuilding = buildingDict[saveBuilding.requiredBuilding];
				}

				if (saveBuilding.requiredTech != null) {
					building.requiredTech = techDict[saveBuilding.requiredTech];
				}
				if (saveBuilding.renderedObsoleteBy != null) {
					building.renderedObsoleteBy = techDict[saveBuilding.renderedObsoleteBy];
				}
				if (saveBuilding.greatWonderProperties?.buildingGainedInEveryCity?.Length > 0) {
					building.greatWonderProperties.buildingGainedInEveryCity = buildingDict[saveBuilding.greatWonderProperties.buildingGainedInEveryCity];
				}
				if (saveBuilding.greatWonderProperties?.buildingGainedInEveryCityOnContinent?.Length > 0) {
					building.greatWonderProperties.buildingGainedInEveryCityOnContinent = buildingDict[saveBuilding.greatWonderProperties.buildingGainedInEveryCityOnContinent];
				}

				building.requiredResources = saveBuilding.requiredResources.Select(a => resDict[a]).ToHashSet();
			}
		}

		private void ConvertUnits(GameData data) {
			data.unitPrototypes = UnitPrototypes.ConvertAll(prototype => new UnitPrototype(prototype, data.Terraforms));

			var techDict = data.techs.ToDictionary(t => t.id);
			var unitPrototypeDict = data.unitPrototypes.ToDictionary(b => b.name);
			var civDict = data.civilizations.ToDictionary(c => c.name);
			var resDict = data.Resources.ToDictionary(r => r.Key);

			foreach (SaveUnitPrototype saveProto in UnitPrototypes) {
				UnitPrototype proto = unitPrototypeDict[saveProto.name];

				if (saveProto.upgradeTo != null) {
					proto.upgradeTo = unitPrototypeDict[saveProto.upgradeTo];
				}

				if (saveProto.requiredTech != null) {
					proto.requiredTech = techDict[saveProto.requiredTech];
				}

				if (saveProto.unique != null) {
					Civilization civ = civDict[saveProto.unique.civilization];

					proto.unique = new() {
						civilization = civ
					};

					if (saveProto.unique.replace != null) {
						proto.unique.replace = unitPrototypeDict[saveProto.unique.replace];
					}

					civ.uniqueUnit = proto;
				}

				proto.requiredResources = saveProto.requiredResources.Select(a => resDict[a]).ToHashSet();
			}

			// map units need game map and players to populate location and owner
			data.mapUnits = Units.ConvertAll(unit =>
				unit.ToMapUnit(data.unitPrototypes, ExperienceLevels, data.players, data.Terraforms, data.map)
			);


			// once unit owners are known, players can reference units
			data.players.ForEach(player => {
				player.units = data.mapUnits.Where(unit => unit.owner.id == player.id).ToList();
			});
		}

		private void ConvertCities(GameData data) {
			// cities require game map for location and players for city owner
			data.cities = Cities.ConvertAll(city =>
				city.ToCity(data.map, data.players, data.unitPrototypes, Civilizations, data.Buildings, CitizenTypes)
			);

			// add references to map tiles after units and cities are defined
			populateGameDataTileUnitsAndCities(data);

			// Once cities are known, players can reference cities.
			data.players.ForEach(player => {
				player.cities = data.cities.Where(city => city.owner.id == player.id).ToList();
			});

			foreach (City city in data.cities) {
				data.map.tileAt(city.location.XCoordinate, city.location.YCoordinate).cityAtTile = city;
			}
		}

		private void ConvertBarbarianInfo(GameData data) {
			data.barbarianInfo = BarbarianInfo;

			if (BarbarianInfo.basicBarbarianUnit != null) {
				data.barbarianInfo.basicBarbarian = data.unitPrototypes.Where(up => up.name == data.barbarianInfo.basicBarbarianUnit).First();
			}
			if (BarbarianInfo.advancedBarbarianUnit != null) {
				data.barbarianInfo.advancedBarbarian = data.unitPrototypes.Where(up => up.name == data.barbarianInfo.advancedBarbarianUnit).First();
			}
			if (BarbarianInfo.barbarianSeaUnit != null) {
				data.barbarianInfo.barbarianSeaUnitProto = data.unitPrototypes.Where(up => up.name == data.barbarianInfo.barbarianSeaUnit).First();
			}
		}

		private void ConvertStrengthBonuses(GameData data) {
			foreach (StrengthBonus sb in StrengthBonuses) {
				switch (sb.description) {
					case "Fortified":
						data.fortificationBonus = sb;
						break;
					case "Behind river":
						data.riverCrossingBonus = sb;
						break;
					case "Town":
						data.cityLevel1DefenseBonus = sb;
						break;
					case "City":
						data.cityLevel2DefenseBonus = sb;
						break;
					case "Metropolis":
						data.cityLevel3DefenseBonus = sb;
						break;
				}
			}
		}

		private void ConvertHealRates(GameData data) {
			data.healRateInFriendlyField = HealRates["friendly_field"];
			data.healRateInNeutralField = HealRates["neutral_field"];
			data.healRateInHostileField = HealRates["hostile_field"];
			data.healRateInCity = HealRates["city"];
		}

		public string Version = "0.0.0";
		public int Seed = -1;
		public int TurnNumber = 0;
		public SaveMap Map = new SaveMap();
		public List<TerrainType> TerrainTypes = new List<TerrainType>();
		public List<SaveTerrainImprovement> TerrainImprovements = [];
		public List<Resource> Resources = new List<Resource>();
		public List<SaveUnit> Units = new List<SaveUnit>();
		public List<SaveUnitPrototype> UnitPrototypes = [];
		public List<SaveBuilding> Buildings = [];
		public HashSet<string> GreatWondersBuilt = new();
		public List<SavePlayer> Players = new List<SavePlayer>();
		public List<SaveCity> Cities = new List<SaveCity>();
		public BarbarianInfo BarbarianInfo = new BarbarianInfo();
		public List<ExperienceLevel> ExperienceLevels = new List<ExperienceLevel>();
		public string DefaultExperienceLevel; // key
		public List<Civilization> Civilizations = new List<Civilization>();
		public List<StrengthBonus> StrengthBonuses = new List<StrengthBonus>();
		public Dictionary<string, int> HealRates = new Dictionary<string, int>();
		public Rules Rules = new();
		public List<SaveTech> Techs = new();
		public List<CitizenType> CitizenTypes = new();
		public List<SaveTerraform> TerraForms = new();
		public List<Government> Governments = new();

		// The relative directory that can be used to find scenario-specific
		// assets.
		public string ScenarioSearchPath;

		public List<Difficulty> Difficulties = new();
		public Difficulty GameDifficulty = new();
		public void Save(string path) {
			byte[] json = JsonSerializer.SerializeToUtf8Bytes(this, JsonOptions);
			File.WriteAllBytes(path, json);
		}

		public static SaveGame Load(string path, Func<string, string> getPediaIconsPath) {
			SaveGame result = JsonSerializer.Deserialize<SaveGame>(File.ReadAllText(path), JsonOptions);

			// This lambda has side effects in the Game.cs code.
			if (result.ScenarioSearchPath?.Count() > 0) {
				getPediaIconsPath(result.ScenarioSearchPath);
			}
			return result;
		}
	}
}
