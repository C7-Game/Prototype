using System;
using System.Collections.Generic;
using System.Linq;
using C7Engine.AI.StrategicAI;

namespace C7GameData {

	public class Player {
		public ID id { get; internal set; }
		public int colorIndex;
		public bool isBarbarians = false;
		//TODO: Refactor front-end so it sends player GUID with requests.
		//We should allow multiple humans, this is a temporary measure.
		public bool isHuman = false;
		public bool hasPlayedThisTurn = false;

		public Civilization civilization;
		internal int cityNameIndex = 0;

		public List<MapUnit> units = new List<MapUnit>();
		public List<City> cities = new List<City>();
		public TileKnowledge tileKnowledge = new TileKnowledge();

		//Ordered list of priority data.  First is most important.
		public List<StrategicPriority> strategicPriorityData = new List<StrategicPriority>();

		// The list of techs known by this player.
		public HashSet<ID> knownTechs = new();

		// The tech the player is currently researching.
		public ID currentlyResearchedTech { get; private set; }

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

		public void AddUnit(MapUnit unit) {
			this.units.Add(unit);
		}

		public void SetCurrentlyResearchedTech(ID id) {
			currentlyResearchedTech = id;

			// Clear out previous progress.
			beakers = 0;
			turnsResearched = 0;
		}

		public string GetNextCityName() {
			string name = civilization.cityNames[cityNameIndex % civilization.cityNames.Count];
			int bonusLoops = cityNameIndex / civilization.cityNames.Count;
			if (bonusLoops % 2 == 1) {
				name = "New " + name;
			}
			int suffix = (bonusLoops / 2) + 1;
			if (suffix > 1) {
				name = name + " " + suffix; //e.g. for bonusLoops = 2, we'll have "Athens 2"
			}
			cityNameIndex++;
			return name;
		}

		public Player() { }

		public bool IsAtPeaceWith(Player other) {
			// Right now it's a free-for-all but eventually we'll implement peace treaties and alliances
			return other == this;
		}

		public bool SitsOutFirstTurn() {
			// TODO: Scenarios can also specify that certain players sit out the first turn. E.g. WW2 in the Pacific
			return isBarbarians;
		}

		// Once we have technologies, not all resources will be known at the start.
		// Eventually, perhaps there will be other gates around resource access as well
		// For now, just always return true, but have this method so we have that structure
		// in place.
		public bool KnowsAboutResource(Resource resource) {
			return true;
		}

		public int RemainingCities() {
			int result = 0;
			foreach (City city in cities) {
				// Destroyed cities have a size of zero.
				if (city.size > 0) {
					++result;
				}
			}
			return result;
		}

		public override string ToString() {
			if (civilization != null)
				return civilization.cityNames.First();
			return "";
		}

		public int EstimateTurnsToResearch(Tech tech) {
			// Cost formula from https://forums.civfanatics.com/threads/research-cost-formula-v1-29f.29485/.
			// Research Cost = [MM * [10*COST * (1 - N/[CL*1.75])]/(CF * 10)] - progress
			//
			// MM = map modifier (tiny=160, small=200, standard=240, large=320, huge=400)
			// COST = tech cost
			// CF = difficulty factor, range 10 (easy) to 6 (hard)
			// N = number of known civs that have discovered the tech
			// CL = civs left in game
			//
			// We also have the min/max turns to research of 4 and 50.
			// TODO: the min/max costs are in the biq, we should load them.
			// TODO: implement the civ-related parts of the equation
			// TODO: figure out what map size we are
			// TODO: See this this whole equation can be configurable
			int beakersPerTurn = 0;
			foreach (City city in cities) {
				beakersPerTurn += (int)Math.Floor(city.CurrentCommerceYield() * city.owner.scienceRate / 10.0);
			}

			if (beakersPerTurn == 0) {
				// No research is happening.
				return int.MaxValue;
			}

			int mapModifier = 160;  // small, to make testing faster
			int difficultyFactor = 10; // easy difficulty
			int researchCost = mapModifier * 10 * tech.Cost / (difficultyFactor * 10);
			int remainingCost = researchCost - beakers;
			int turnsRemaining = (int)Math.Ceiling((double)remainingCost / beakersPerTurn);

			// We never spend more than 50 turns per tech.
			int maxTurnsRemaining = 50 - turnsResearched;

			int result = Math.Min(turnsRemaining, maxTurnsRemaining);

			// Ensure every tech takes at least 4 turns.
			if (result < 4 && turnsResearched < 4) {
				return 4;
			}
			return result;
		}
	}

}
