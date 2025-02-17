using System;
using System.Collections.Generic;
using Serilog;

namespace C7GameData {
	public class GameData {
		private static ILogger log = Log.ForContext<GameData>();

		public int seed = -1;   //change here to set a hard-coded seed
		public int turn { get; set; }
		public static Random rng; // TODO: Is GameData really the place for this?
		public ID.Factory ids = new();
		public GameMap map { get; set; }
		public List<Player> players = new List<Player>();
		public List<TerrainType> terrainTypes = new List<TerrainType>();
		public List<Resource> Resources = new List<Resource>();
		public List<MapUnit> mapUnits { get; set; } = new List<MapUnit>();
		public List<UnitPrototype> unitPrototypes = new();
		public List<Building> Buildings = new();
		public List<City> cities = new List<City>();

		internal List<Civilization> civilizations = new List<Civilization>();

		public List<ExperienceLevel> experienceLevels = new List<ExperienceLevel>();
		public List<Tech> techs = new();
		public List<CitizenType> citizenTypes = new();
		public string defaultExperienceLevelKey;
		public ExperienceLevel defaultExperienceLevel;

		public BarbarianInfo barbarianInfo = new BarbarianInfo();

		public StrengthBonus fortificationBonus;
		public StrengthBonus riverCrossingBonus;
		public StrengthBonus cityLevel1DefenseBonus;
		public StrengthBonus cityLevel2DefenseBonus;
		public StrengthBonus cityLevel3DefenseBonus;

		public int healRateInFriendlyField;
		public int healRateInNeutralField;
		public int healRateInHostileField;
		public int healRateInCity;

		public bool observerMode = false;

		public string scenarioSearchPath;   //legacy from Civ3, we'll probably have a more modern format someday but this keeps legacy compatibility

		public GameData() {
			map = new GameMap();
			if (seed == -1) {
				rng = new Random();
				seed = rng.Next(int.MaxValue);
				log.Information("Random seed is " + seed);
			}
			rng = new Random(seed);
		}

		public List<Player> GetHumanPlayers() {
			return players.FindAll(p => p.isHuman);
		}

		public MapUnit GetUnit(ID id) {
			return mapUnits.Find(u => u.id == id);
		}

		public Player GetPlayer(ID id) {
			return players.Find(p => p.id == id);
		}

		public ExperienceLevel GetExperienceLevelAfter(ExperienceLevel experienceLevel) {
			int n = experienceLevels.IndexOf(experienceLevel);
			if (n + 1 < experienceLevels.Count)
				return experienceLevels[n + 1];
			else
				return null;
		}

		// TODO: This is a placeholder method for calculating tile owners.
		// Currently, it marks a tile as owned only if it is a city tile or adjacent to a city.
		public void UpdateTileOwners() {
			foreach (City city in cities) {
				if (city.size == 0) {
					continue; // skip destroyed cities
				}

				city.location.owner = city.owner;

				foreach (Tile tile in city.location.neighbors.Values) {
					tile.owner = city.owner;
				}
			}
		}

		public void UpdateTileOwnersOnCityDestruction(City city) {
			city.location.owner = null;

			foreach (Tile tile in city.location.neighbors.Values) {
				tile.owner = null;
			}

			UpdateTileOwners();
		}

		/**
		 * This is intended as a place to set up post-load actions on the save, regardless of
		 * whether it is loaded from a legacy Civ3 file or a C7 native file.
		 * This likely is any sort of calculation which is useful to have in the game state, but
		 * can be re-generated from save data and does not make sense to serialize.
		 **/
		public void PerformPostLoadActions() {
			//Let each tile know who its neighbors are.  It needs to know this so its graphics can be selected appropriately.
			map.computeNeighbors();
		}
	}
}
