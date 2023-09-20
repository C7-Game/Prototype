using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

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

		public SaveGame() {}

		public static SaveGame FromGameData(GameData data) {
			SaveGame save = new SaveGame{
				Seed = data.seed,
				Civilizations = data.civilizations,
				Map = new SaveMap(data.map),
				TerrainTypes = data.terrainTypes,
				Resources = data.Resources,
				BarbarianInfo = data.barbarianInfo,
				Units = data.mapUnits.ConvertAll(unit => new SaveUnit(unit, data.map)),
				UnitPrototypes = data.unitPrototypes.Values.ToList(),
				Players = data.players.ConvertAll(player => new SavePlayer(player)),
				Cities = data.cities.ConvertAll(city => new SaveCity(city)),
				ExperienceLevels = data.experienceLevels,
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
			save.ScenarioSearchPath = data.scenarioSearchPath;
			save.DefaultExperienceLevel = data.defaultExperienceLevelKey;
			return save;
		}

		private void populateGameDataTileUnitsAndCities(GameData data) {
			foreach (Tile tile in data.map.tiles) {
				tile.unitsOnTile = data.mapUnits.Where(unit => unit.location == tile).ToList();
				tile.cityAtTile = data.cities.Find(city => city.location == tile);
			}
		}

		// TODO: GameData should store Civilizations, otherwise the round trip from
		// SaveGame to GameData and back loses Civilization instances that are not
		// assigned to a player.
		public GameData ToGameData() {
			// copy data without references
			GameData data = new GameData{
				seed = Seed,
				terrainTypes = TerrainTypes,
				Resources = Resources,
				unitPrototypes = UnitPrototypes.ToDictionary(up => up.name),
				scenarioSearchPath = ScenarioSearchPath,
				civilizations = Civilizations,
			};
			// units and cities are empty
			data.map = Map.ToGameMap(data);

			// players need game map to populate tile knowledge
			data.players = Players.ConvertAll(player => player.ToPlayer(data.map, Civilizations));

			// map units need game map and players to populate location and owner
			data.mapUnits = Units.ConvertAll(unit => unit.ToMapUnit(UnitPrototypes, ExperienceLevels, data.players, data.map));

			// once unit owners are known, players can reference units
			data.players.ForEach(player => {
				player.units = data.mapUnits.Where(unit => unit.owner.id == player.id).ToList();;
			});

			// cities require game map for location and players for city owner
			data.cities = Cities.ConvertAll(city => city.ToCity(data.map, data.players, UnitPrototypes, Civilizations));
			foreach (City city in data.cities) {
				data.map.tileAt(city.location.xCoordinate, city.location.yCoordinate).cityAtTile = city;
			}

			// add references to map tiles after units and cities are defined
			populateGameDataTileUnitsAndCities(data);

			data.experienceLevels = ExperienceLevels;
			data.barbarianInfo = BarbarianInfo;

			if (BarbarianInfo.basicBarbarianIndex != -1) {
				data.barbarianInfo.basicBarbarian = UnitPrototypes[data.barbarianInfo.basicBarbarianIndex];
			}
			if (BarbarianInfo.advancedBarbarianIndex != -1) {
				data.barbarianInfo.advancedBarbarian = UnitPrototypes[data.barbarianInfo.advancedBarbarianIndex];
			}
			if (BarbarianInfo.barbarianSeaUnitIndex != -1) {
				data.barbarianInfo.barbarianSeaUnit = UnitPrototypes[data.barbarianInfo.barbarianSeaUnitIndex];
			}

			data.defaultExperienceLevelKey = DefaultExperienceLevel;
			data.defaultExperienceLevel = data.experienceLevels.Find(el => el.key == DefaultExperienceLevel);

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

			data.healRateInFriendlyField = HealRates["friendly_field"];
			data.healRateInNeutralField = HealRates["neutral_field"];
			data.healRateInHostileField = HealRates["hostile_field"];
			data.healRateInCity = HealRates["city"];

			return data;
		}

		public string Version = "0.0.0";
		public int Seed = -1;
		public SaveMap Map = new SaveMap();
		public List<TerrainType> TerrainTypes = new List<TerrainType>();
		public List<Resource> Resources = new List<Resource>();
		public List<SaveUnit> Units = new List<SaveUnit>();
		public List<UnitPrototype> UnitPrototypes = new List<UnitPrototype>();
		public List<SavePlayer> Players = new List<SavePlayer>();
		public List<SaveCity> Cities = new List<SaveCity>();
		public BarbarianInfo BarbarianInfo = new BarbarianInfo();
		public List<ExperienceLevel> ExperienceLevels = new List<ExperienceLevel>();
		public string DefaultExperienceLevel; // key
		public List<Civilization> Civilizations = new List<Civilization>();
		public List<StrengthBonus> StrengthBonuses = new List<StrengthBonus>();
		public Dictionary<string, int> HealRates = new Dictionary<string, int>();
		public string ScenarioSearchPath; // TODO: what is this
		public void Save(string path) {
			byte[] json = JsonSerializer.SerializeToUtf8Bytes(this, JsonOptions);
			File.WriteAllBytes(path, json);
		}

		public static SaveGame Load(string path) {
			return JsonSerializer.Deserialize<SaveGame>(File.ReadAllText(path), JsonOptions);
		}
	}
}
