using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Serilog;
using C7Engine.Lua;
using C7Engine.Pathing;
using System.Threading.Tasks;
using C7Engine;

[assembly: InternalsVisibleTo("EngineTests")]
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
		public List<Inflow> Inflows = new();

		// The names of all great wonders that have been built.
		public HashSet<string> GreatWondersBuilt = new();

		public List<City> cities = new List<City>();

		internal List<Civilization> civilizations = new List<Civilization>();
		internal HashSet<CultureGroup> cultureGroups = new HashSet<CultureGroup>();

		public List<ExperienceLevel> experienceLevels = new List<ExperienceLevel>();
		public List<Tech> techs = new();
		public List<CitizenType> citizenTypes = new();
		public List<Terraform> Terraforms = new();
		public List<Government> governments = new();
		public List<WorldSize> worldSizes = new();
		public List<Difficulty> difficulties = new();
		public Difficulty gameDifficulty = new();
		public string defaultExperienceLevelKey;
		public ExperienceLevel defaultExperienceLevel;
		public Rules rules;
		public TimeOptions timeOptions;

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

		internal RulesEngine luaRulesEngine = new();

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
					if (t.owningCity != null && ResolveTileOwnershipConflict(t.owningCity, city, t, out City winnerCity)) {
						t.owningCity = winnerCity;
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

				foreach (Tile t in player.tileKnowledge.knownTiles.Where(t => t.owningCity == null && t.GetEdgeNeighbors().Any(e => e.owningCity != null)).ToList()) {
					// Law VII
					TryResolveOpposingNeighbors(t, TileDirection.NORTHWEST, TileDirection.SOUTHEAST);
					if (t.owningCity != null) continue;
					// Law VIII
					TryResolveOpposingNeighbors(t, TileDirection.NORTHEAST, TileDirection.SOUTHWEST);
				}
			}
		}

		private void TryResolveOpposingNeighbors(Tile t, TileDirection dirA, TileDirection dirB) {
			if (!t.neighbors.TryGetValue(dirA, out Tile a) || !t.neighbors.TryGetValue(dirB, out Tile b)) return;
			if (a.owningCity == null || b.owningCity == null) return;
			if (a.owningCity.owner != b.owningCity.owner) return;
			if (!ResolveTileOwnershipConflict(a.owningCity, b.owningCity, t, out City winnerCity)) return;

			// Law II
			if (t.baseTerrainType.Key == "ocean" && t.rankDistanceTo(winnerCity.location) > 2) {
				t.owningCity = null;
				return;
			}
			t.owningCity = winnerCity;
			winnerCity.owner.tileKnowledge.AddTilesToKnown(t);
		}

		public void UpdateTileOwnersOnCityDestruction(City city) {
			city.location.owningCity = null;

			var borderTiles = city.GetTilesWithinBorders();
			var borderTileIds = borderTiles.Select(x => x.Id).ToHashSet();

			foreach (Tile tile in borderTiles) {
				tile.owningCity = null;

				// Aggressively remove ownership from edge neighbors around the city outside the natural border.
				// This clears out tile ownership due to Law VII or Law VIII.
				// Regular tile ownership update will re-assign ownership where needed.
				foreach (var fringeTile in tile.GetEdgeNeighbors().Where(x => !borderTileIds.Contains(x.Id))) {
					if (fringeTile.HasCity) // skip encountered cities, just in case 
						continue;

					fringeTile.owningCity = null;
				}
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
			// Start from the end and start deleting backwards
			// because the other way doesn't actually go through all units
			// probably because we keep modifying the list and its count gets all messed up
			for (int i = player.units.Count - 1; i >= 0; --i) {
				RemoveUnit(player.units[i]);
			}

			player.defeated = true;

			// Remove this civ from all other player's relationships.
			foreach (Player p in players) {
				p.playerRelationships.Remove(player.id);
			}

			return true;
		}

		[LuaMethod]
		public void SendMessageToUiFromLua(string message, Tile location) {
			log.Information($"[LUA API] - {message}");
			new MsgShowTemporaryPopup(message, location).send();

		}

		public async Task DisbandUnit(MapUnit unit) {
			log.Information($"Player {unit.owner} disbands unit: {unit}");

			var disbandFunction = this.luaRulesEngine.ImportFunc<Action<MapUnit>>("gameplay.units.disband", true);
			disbandFunction.Invoke(unit);

			await unit.animateAsync(MapUnit.AnimatedAction.DEATH, AnimationEnding.Pause);

			RemoveUnit(unit);
		}

		internal void RemoveUnit(MapUnit unit) {
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

			Player owner = unit.owner;
			owner.units.Remove(unit);

			log.Information($"Player {owner} removed unit: {unit}");
		}

		internal void SpawnUnit(Player player, UnitPrototype proto, Tile tile) {
			// TODO: consolidate unit spawning routines (here) 

			MapUnit newUnit = proto.GetInstance(this.GenerateID(proto.name), proto, player, location: tile);
			// TODO: make this a conscript.
			newUnit.experienceLevelKey = defaultExperienceLevelKey;
			newUnit.experienceLevel = defaultExperienceLevel;
			newUnit.hitPointsRemaining = 3;

			tile.unitsOnTile.Add(newUnit);
			mapUnits.Add(newUnit);
			player.units.Add(newUnit);

			log.Debug("New unit of type {type} added at {tile} for player {player}",
				proto.name, tile, player);
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
		private bool ResolveTileOwnershipConflict(City a, City b, Tile t, out City owner) {
			owner = null;
			if (a.Equals(b)) { owner = a; return true; }

			int aRank = a.location.rankDistanceTo(t);
			int bRank = b.location.rankDistanceTo(t);

			// Law I
			// Cities can claim tiles of rank n+1, where n is the city's expansion level
			if (a.GetBorderExpansionLevel() + 1 < aRank && b.GetBorderExpansionLevel() + 1 >= bRank) { owner = b; return true; }
			if (b.GetBorderExpansionLevel() + 1 < bRank && a.GetBorderExpansionLevel() + 1 >= aRank) { owner = a; return true; }

			// Law III
			// The city with the lowest rank claim gets the tile.
			if (aRank > bRank) { owner = b; return true; }
			if (aRank < bRank) { owner = a; return true; }

			// Law IV
			// If the ranks are equal, the city with more culture gets the tile.
			if (a.GetCulture() + a.GetCulturePerTurn() < b.GetCulture() + b.GetCulturePerTurn()) { owner = b; return true; }
			if (a.GetCulture() + a.GetCulturePerTurn() > b.GetCulture() + b.GetCulturePerTurn()) { owner = a; return true; }

			// Law V
			// If the cultures are equal the oldest city gets the tile.
			// TODO: track city age - for now we are going to skip this.
			// return a;

			// Law VI
			// Starting North of the disputed tile, we go counter-clockwise
			// trying to find the first tile that has one of the competing cities.
			// We start at (rank - 1) because the rank distance does not necessarily reflect the actual "ring"
			// the city tile is in, so a tile at rank 3, could well mean it's in the 2nd ring.
			for (int r = aRank - 1; r <= aRank; r++) {
				if (r <= 0) continue;
				Tile winner = t.FindInRing(r, tile => tile.HasCity && (tile.cityAtTile == a || tile.cityAtTile == b), false);
				if (winner == null) continue;
				owner = winner.owningCity;
				return true;
			}

			// should never happen, if it does some part of the algorithm has gone wrong
			throw new Exception($"Failed to resolve ownership of {t} between {a.name} and {b.name}, something went wrong");
		}
	}

	public static class GameDataUtils {
		public static ID GenerateID(this GameData gameData, string identifier) {
			return gameData.ids.CreateID(identifier);
		}
	}
}
