using System.Collections.Generic;
using System.Linq;

namespace C7GameData.Save {
	// A class holding all the state of the relationship between two civs.
	public class PlayerRelationship {
		public bool atWar = false;

		// p1.playerRelationships[p2].warDeclarationCount is the number of times
		// p2 declared war on p1.
		public int warDeclarationCount = 0;

		// true if a war declaration happened with units inside the player's
		// borders.
		public bool wasSneakAttacked = false;

		// If at war, refuse contact with the relevant player until this turn
		// has been reached.
		public int refuseContactUntilTurn = -1;
	}

	public class SavePlayer {
		public ID id;
		public int colorIndex;
		public bool human = false;
		public bool hasPlayedCurrentTurn = false;
		public bool defeated = false;

		public string civilization;
		public int cityNameIndex = 0;

		public List<TileLocation> tileKnowledge = new List<TileLocation>();

		// A map from player id to the relationship this player has with the other player.
		public Dictionary<string, PlayerRelationship> playerRelationships = new();

		// The list of techs known by this player.
		public HashSet<ID> knownTechs = new();

		// The tech the player is currently researching.
		public ID currentlyResearchedTech;

		// The tech queue the player is currently researching.
		public List<ID> researchQueue = new();

		// The civilopedia name of the era this player is in.
		//
		// The civilopedia name is what is used for art lookups, not the actual
		// name.
		public string eraCivilopediaName;

		public int turnsUntilPriorityReevaluation = 0;

		// The values of the science/happiness/tax sliders (tax is implicit)
		// A value of 1 => 10%, a value of 10 => 100%.
		//
		// INVARIANT: LuxuryRate + ScienceRate + TaxRate = 10
		public int luxuryRate = 0;
		public int scienceRate = 5;
		public int taxRate = 5;

		// The amount of gold this player has.
		public int gold = 0;

		// The number of "beakers" (gold) spent on the currently researched
		// tech.
		public int beakers = 0;

		// The number of turns the player has been researching the current tech.
		public int turnsResearched = 0;

		// If the government is anarchy (or a govt with the transition bool set
		// to true), the turn number at which switching governments is allowed.
		public int inAnarchyUntilTurn = 0;

		// The current government of the player.
		public ID governmentId;

		public Player ToPlayer(GameMap map, List<Civilization> civilizations, List<Government> governments, List<Tech> techs, Rules rules) {
			Player player = new Player{
				id = id,
				isHuman = human,
				hasPlayedThisTurn = hasPlayedCurrentTurn,
				defeated = defeated,
				colorIndex = colorIndex,
				civilization = civilization is not null ? civilizations.Find(civ => civ.name == civilization) : null,
				cityNameIndex = cityNameIndex,
				knownTechs = knownTechs,
				eraCivilopediaName = eraCivilopediaName,
				luxuryRate = luxuryRate,
				scienceRate = scienceRate,
				taxRate = taxRate,
				gold = gold,
				turnsUntilPriorityReevaluation = turnsUntilPriorityReevaluation,
				inAnarchyUntilTurn = inAnarchyUntilTurn,
				government = governments.Find(x => x.id == governmentId),
				rules = rules,
			};
			foreach (TileLocation tile in tileKnowledge) {
				player.tileKnowledge.AddTileToKnown(map.tileAt(tile.X, tile.Y));
			}
			foreach (ID techId in player.civilization.startingTechs) {
				if (!player.knownTechs.Contains(techId)) {
					player.knownTechs.Add(techId);
				}
			}
			foreach (KeyValuePair<string, PlayerRelationship> keyValuePair in this.playerRelationships) {
				player.playerRelationships.Add(ID.FromString(keyValuePair.Key), keyValuePair.Value);
			}

			// Because of the custom setter we need to set the researched tech
			// and then set the beakers and turns researched - otherwise they'd
			// be reset by the setter.
			player.SetCurrentlyResearchedTech(currentlyResearchedTech);
			player.beakers = beakers;
			player.turnsResearched = turnsResearched;

			foreach (ID techId in researchQueue) {
				Tech tech = techs.Find(x => x.id == techId);
				player.AddTechItemToResearchQueue(tech);
			}

			return player;
		}

		public SavePlayer() { }

		public SavePlayer(Player player) {
			id = player.id;
			colorIndex = player.colorIndex;
			human = player.isHuman;
			hasPlayedCurrentTurn = player.hasPlayedThisTurn;
			defeated = player.defeated;
			civilization = player.civilization?.name;
			// TODO: this should be computed by looking at cities defined in the save
			// so that adding cities in the save structure doesn't require updating this value
			cityNameIndex = player.cityNameIndex;
			tileKnowledge = player.tileKnowledge.AllKnownTiles().ConvertAll(tile => new TileLocation(tile));
			turnsUntilPriorityReevaluation = player.turnsUntilPriorityReevaluation;
			knownTechs = player.knownTechs;
			currentlyResearchedTech = player.currentlyResearchedTech;
			researchQueue = new List<ID>(player.ResearchQueue.Select(t => t.id));
			eraCivilopediaName = player.eraCivilopediaName;
			luxuryRate = player.luxuryRate;
			scienceRate = player.scienceRate;
			taxRate = player.taxRate;
			gold = player.gold;
			beakers = player.beakers;
			turnsResearched = player.turnsResearched;
			inAnarchyUntilTurn = player.inAnarchyUntilTurn;
			governmentId = player.government.id;

			foreach (KeyValuePair<ID, PlayerRelationship> keyValuePair in player.playerRelationships) {
				playerRelationships.Add(keyValuePair.Key.ToString(), keyValuePair.Value);
			}
		}
	}
}
