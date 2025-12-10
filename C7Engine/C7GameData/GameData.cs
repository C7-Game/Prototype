using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using C7Engine;
using C7Engine.Pathing;

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
		public List<TerrainImprovement> terrainImprovements = [];
		public List<Resource> Resources = new List<Resource>();
		public List<MapUnit> mapUnits { get; set; } = new List<MapUnit>();
		public List<UnitPrototype> unitPrototypes = new();
		public List<Building> Buildings = new();

		// The names of all great wonders that have been built.
		public HashSet<string> GreatWondersBuilt = new();

		public List<City> cities = new List<City>();

		internal List<Civilization> civilizations = new List<Civilization>();

		public List<ExperienceLevel> experienceLevels = new List<ExperienceLevel>();
		public List<Tech> techs = new();
		public List<CitizenType> citizenTypes = new();
		public List<Terraform> Terraforms = new();
		public List<Government> governments = new();
		public List<Difficulty> difficulties = new();
		public Difficulty gameDifficulty = new();
		public string defaultExperienceLevelKey;
		public ExperienceLevel defaultExperienceLevel;
		public Rules rules;

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
		public bool showGridCoordinates = false;

		public string scenarioSearchPath;   //legacy from Civ3, we'll probably have a more modern format someday but this keeps legacy compatibility

		internal LuaRulesEngine luaRulesEngine = new();

		// The cached trade network for all players. This is invalidated whenever
		// a road is built or a city is created/destroyed.
		private TradeNetwork tradeNetwork;

		// An action called after initialization of EngineStorage
		internal Action onGameCreation;

		public GameData() {
			map = new GameMap();
			if (seed == -1) {
				rng = new Random();
				seed = rng.Next(int.MaxValue);
				log.Information("Random seed is " + seed);
			}
			rng = new Random(seed);
		}

		// Returns the first human player in the set of players, or the first
		// non-barbarian player if we're in observer mode (where the human player
		// is no longer marked as human).
		public Player GetFirstHumanPlayer() {
			foreach (Player p in players) {
				if (p.isHuman) {
					return p;
				}
			}

			if (observerMode) {
				foreach (Player p in players) {
					if (!p.isBarbarians) {
						return p;
					}
				}
			}

			return null;
		}

		public MapUnit GetUnit(ID id) {
			return mapUnits.Find(u => u.id == id);
		}

		public Player GetPlayer(ID id) {
			return players.Find(p => p.id == id);
		}

		public Tech GetTech(ID id) {
			return techs.Find(p => p.id == id);
		}

		public ExperienceLevel GetExperienceLevelAfter(ExperienceLevel experienceLevel) {
			int n = experienceLevels.IndexOf(experienceLevel);
			if (n + 1 < experienceLevels.Count)
				return experienceLevels[n + 1];
			else
				return null;
		}

		public void UpdateTileOwners() {
			// We do this at the end of the method - we don't need to do this
			// for each tile we add in the loop below.
			bool recomputeActiveTiles = false;

			foreach (City city in cities) {
				if (city.residents.Count == 0) {
					continue; // skip destroyed cities
				}

				city.location.owningCity = city;

				foreach (Tile t in city.GetTilesWithinBorders()) {
					// If another city has claim to this tile, we need to resolve
					// that conflict.
					if (t.owningCity != null) {
						t.owningCity = ResolveTileOwnershipConflict(t.owningCity, city, t);
						t.owningCity.owner.tileKnowledge.AddTilesToKnown(t, recomputeActiveTiles);
						continue;
					}

					t.owningCity = city;
					t.owningCity.owner.tileKnowledge.AddTilesToKnown(t, recomputeActiveTiles);
				}
			}

			foreach (Player player in players) {
				player.tileKnowledge.RecomputeActiveTiles();
				player.UpdateResourcesInBorders(map.tiles.Where(t => t.owningCity?.owner == player));
			}
		}

		public void UpdateTileOwnersOnCityDestruction(City city) {
			city.location.owningCity = null;

			foreach (Tile tile in city.GetTilesWithinBorders()) {
				tile.owningCity = null;
			}

			UpdateTileOwners();
		}

		public bool CheckForCivDestruction(Player player) {
			// TODO: Implement the full set of conditions for destroying a civ;
			// handling cases like 1 city elimination, regicide, settlers that
			// are still alive, etc.
			if (player.RemainingCities() > 0) {
				return false;
			}

			// This was the last city of the civilization, so destroy remaining
			// units.
			player.defeated = true;
			for (int i = 0; i < player.units.Count; ++i) {
				DisbandUnit(player.units[i]);
			}

			// Remove this civ from all other player's relationships.
			foreach (Player p in players) {
				p.playerRelationships.Remove(player.id);
			}

			return true;
		}

		public void DisbandUnit(MapUnit unit) {
			// Set unit's hit points to zero to indicate that it's no longer alive. Ultimately we may not want to do this. I'm only doing it right
			// now since this way all the UI needs to do to check if the selected unit has been destroyed is to check its hit points.
			unit.hitPointsRemaining = 0;
			unit.movementPoints.onConsumeAll();

			if (unit.currentAI != null) {
				unit.currentAI.UpdateOnDeath();
				unit.currentAI = null;
			}

			// EngineStorage.animTracker.endAnimation(unit, false);   TODO: Must send message instead of call directly
			unit.location.unitsOnTile.Remove(unit);
			mapUnits.Remove(unit);

			// TODO: why not just do Player p = unit.owner; p.units.Remove(unit);?
			foreach (Player player in players) {
				if (player.units.Contains(unit)) {
					player.units.Remove(unit);
				}
			}
		}

		public int TechCostFor(Tech tech, Player player) {
			// Cost formula from https://forums.civfanatics.com/threads/research-cost-formula-v1-29f.29485/.
			// Research Cost = [MM * [10*COST * (1 - N/[CL*1.75])]/(CF * 10)] - progress
			//
			// MM = map modifier (tiny=160, small=200, standard=240, large=320, huge=400)
			// COST = tech cost
			// CF = difficulty factor
			// N = number of known civs that have discovered the tech
			// CL = civs left in game
			//
			// We also have the min/max turns to research of 4 and 50 (defined
			// in the rules)
			// TODO: See this this whole equation can be configurable
			int knownCivsThatKnowTheTech = 0;
			int civsLeft = 0;
			foreach (Player p in players) {
				if (player.playerRelationships.ContainsKey(p.id) && p.knownTechs.Contains(tech.id)) {
					++knownCivsThatKnowTheTech;
				}
				if (!p.isBarbarians && !p.defeated) {
					++civsLeft;
				}
			}

			int difficultyFactor = player.isHuman ? gameDifficulty.HumanCostFactor : gameDifficulty.AiCostFactor;
			float knowledgeFactor = 1.0f - knownCivsThatKnowTheTech / (civsLeft * 1.75f);
			float researchCost = map.techRate * 10 * tech.Cost  * knowledgeFactor/ (difficultyFactor * 10);

			// Only include the progress factor if this is the tech actively
			// being researched.
			if (player.currentlyResearchedTech == tech.id) {
				researchCost -= player.beakers;
			}

			return (int)Math.Max(Math.Floor(researchCost), 0);
		}

		public TradeNetwork GetTradeNetwork() {
			if (tradeNetwork == null) {
				tradeNetwork = new(this);
			}
			return tradeNetwork;
		}

		public void InvalidateCachedTradeNetwork() {
			tradeNetwork = null;
		}

		// Rules taken from https://forums.civfanatics.com/threads/the-eight-laws-of-border-dynamics.106882/
		private City ResolveTileOwnershipConflict(City a, City b, Tile t) {
			int aRank = a.location.rankDistanceTo(t);
			int bRank = b.location.rankDistanceTo(t);

			// The city with the lowest rank claim gets the tile.
			if (aRank > bRank) {
				return b;
			} else if (aRank < bRank) {
				return a;
			}

			// If the ranks are equal, the city with more culture gets the tile.
			if (a.GetCulture() < b.GetCulture()) {
				return b;
			} else if (a.GetCulture() > b.GetCulture()) {
				return a;
			}

			// If the cultures are equal the oldest city gets the tile.
			// TODO: track city age - for now we just return the first.
			return a;
		}
	}
}
