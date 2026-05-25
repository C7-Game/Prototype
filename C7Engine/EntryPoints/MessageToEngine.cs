using System.Threading.Tasks;
using Serilog;

namespace C7Engine {
	using System;
	using C7GameData;

	public abstract class MessageToEngine {
		public abstract void process();

		public void send() {
			EngineStorage.pendingMessages.Enqueue(this);
		}
	}

	public class MsgShutdownEngine : MessageToEngine {
		private ILogger log = Log.ForContext<MsgShutdownEngine>();

		public override void process() {
			log.Information("Engine received shutdown message.");
		}
	}

	public class MsgSetFortification : MessageToEngine {
		private ID unitID;
		private bool fortifyElseWake;

		public MsgSetFortification(ID unitID, bool fortifyElseWake) {
			this.unitID = unitID;
			this.fortifyElseWake = fortifyElseWake;
		}

		public override void process() {
			MapUnit unit = EngineStorage.gameData.GetUnit(unitID);

			// Simply do nothing if we weren't given a valid GUID. TODO: Maybe this is an error we need to handle? In an MP game, we should reject
			// invalid actions at the server level but at the client level an invalid action received from the server indicates a desync.
			if (unit != null) {
				if (fortifyElseWake)
					unit.fortify();
				else
					unit.wake();
			}
		}
	}

	public class MsgMoveUnit : MessageToEngine {
		private ID unitID;
		private TileDirection dir;

		public MsgMoveUnit(ID unitID, TileDirection dir) {
			this.unitID = unitID;
			this.dir = dir;
		}

		public override async void process() {
			MapUnit unit = EngineStorage.gameData.GetUnit(unitID);
			if (unit == null) return;

			await unit.move(dir, true);
		}
	}

	public class MsgSetUnitPath : MessageToEngine {
		private ID unitID;
		private TilePath path;

		public MsgSetUnitPath(ID unitID, TilePath path) {
			this.unitID = unitID;
			this.path = path;
		}

		public override async void process() {
			MapUnit unit = EngineStorage.gameData.GetUnit(unitID);
			if (unit == null) return;

			await unit.setUnitPath(path);
		}
	}

	public class MsgBombard : MessageToEngine {
		private ID unitID;
		private readonly int tileX;
		private readonly int tileY;


		public MsgBombard(ID unitID, Tile tile) {
			this.unitID = unitID;
			this.tileX = tile.XCoordinate;
			this.tileY = tile.YCoordinate;
		}

		public override async void process() {
			MapUnit unit = EngineStorage.gameData.GetUnit(unitID);
			Tile tile = EngineStorage.gameData.map.tileAt(tileX, tileY);
			if (unit == null || tile == null) return;

			await unit.bombard(tile);
		}
	}

	// A generic class that allows the UI to have the game engine run some
	// action, assumed to be on a unit.
	//
	// Actions that require more than a 1 or 2 line lambda should probably use
	// a custom subclass.
	public class ActionToEngineMsg : MessageToEngine {
		private Func<Task> action;
		public ActionToEngineMsg(Func<Task> action) {
			this.action = action;
		}
		public ActionToEngineMsg(Action action) {
			this.action = () => {
				action();
				return Task.CompletedTask;
			};
		}

		public override void process() {
			action();
		}
	}

	// Switches the player government to anarchy and determines when the player
	// can exit anarchy.
	public class StartGovernmentTransitionMsg : MessageToEngine {
		private Player player;

		public StartGovernmentTransitionMsg(Player p) {
			player = p;
		}

		public override void process() {
			GameData gD = EngineStorage.gameData;
			Government transitionGovt = gD.governments.Find(x => x.transitionType);
			player.government = transitionGovt;
			player.inAnarchyUntilTurn = gD.turn + player.GetTurnsOfAnarchyForTransition(gD);

			// Update the domestic advisor once we know how long the anarchy is.
			new MsgUpdateUiAfterDomesticChange().send();
		}
	}

	public class SelectGovernmentMsg : MessageToEngine {
		private Player player;
		private Government government;

		public SelectGovernmentMsg(Player player, Government government) {
			this.player = player;
			this.government = government;
		}

		public override void process() {
			player.government = government;
		}
	}

	// A Class that allows the UI to have the game engine run some
	// terraform action.
	public class MsgStartWorkerJob : MessageToEngine {
		private ID UnitID;
		private Terraform Action;
		public MsgStartWorkerJob(ID unitID, Terraform action) {
			this.UnitID = unitID;
			this.Action = action;
		}

		public override void process() {
			MapUnit unit = EngineStorage.gameData.GetUnit(UnitID);
			unit?.PerformTerraformAction(Action);
		}
	}

	public class MsgChooseProduction : MessageToEngine {
		private ID cityID;
		private string producibleName;

		public MsgChooseProduction(ID cityID, string producibleName) {
			this.cityID = cityID;
			this.producibleName = producibleName;
		}

		public override void process() {
			City city = EngineStorage.gameData.cities.Find(c => c.id == cityID);
			if (city != null) {
				foreach (IProducible producible in city.ListProductionOptions(EngineStorage.gameData)) {
					if (producible.name == producibleName) {
						city.SetItemBeingProduced(producible);
						break;
					}
				}
			}
		}
	}

	public class MsgChooseResearch : MessageToEngine {
		private Tech tech;
		private AdvisorState advisorState;
		private SelectionMode selectionMode;

		public enum AdvisorState : byte {
			DontShow,
			Show,
		}
		public enum SelectionMode : byte {
			Single,
			Multi,
		}

		public MsgChooseResearch(Tech tech, AdvisorState advisorState, SelectionMode selectionMode = SelectionMode.Single) {
			this.tech = tech;
			this.advisorState = advisorState;
			this.selectionMode = selectionMode;
		}

		public override void process() {
			Player player = EngineStorage.gameData.GetFirstHumanPlayer();

			bool isTechEraBeyondPlayerEra = EraUtils.GetEraIndex(tech.EraCivilopediaName) > EraUtils.GetEraIndex(player.eraCivilopediaName);
			if (player.knownTechs.Contains(tech.id) || isTechEraBeyondPlayerEra)
				return;
			if (player.currentlyResearchedTech == tech.id && player.ResearchQueue.Count == 1) {
				return;
			}

			// Start the tech queueing process
			// and start researching the first tech in the queue
			// or append a new queue to the current one if it's a multiselection process
			if (selectionMode == SelectionMode.Single) {
				player.CalculateFreshTechQueueAndAssignNewCurrent(tech);
			} else {
				player.CalculateTechQueueAndAppendToCurrentQueue(tech);

			}

			// update the UI
			if (advisorState == AdvisorState.Show) {

				new MsgShowScienceAdvisor().send();
			}
		}
	}

	public class MsgChangeSliders : MessageToEngine {
		private bool moreScience;
		private bool lessScience;
		private bool moreLuxury;
		private bool lessLuxury;

		public MsgChangeSliders(bool moreScience, bool lessScience, bool moreLuxury, bool lessLuxury) {
			this.moreScience = moreScience;
			this.lessScience = lessScience;
			this.moreLuxury = moreLuxury;
			this.lessLuxury = lessLuxury;
		}

		public override void process() {
			Player player = EngineStorage.gameData.GetFirstHumanPlayer();

			if (moreScience && player.scienceRate == 10 || lessScience && player.scienceRate == 0) {
				return;
			}
			if (moreLuxury && player.luxuryRate == 10 || lessLuxury && player.luxuryRate == 0) {
				return;
			}

			// Increase our science rate, taking away from tax rate if we can,
			// otherwise decrease the luxury rate.
			if (moreScience) {
				player.scienceRate++;
				if (player.taxRate > 0) {
					player.taxRate--;
				} else {
					player.luxuryRate--;
				}
			}

			// Ditto for luxury.
			if (moreLuxury) {
				player.luxuryRate++;
				if (player.taxRate > 0) {
					player.taxRate--;
				} else {
					player.scienceRate--;
				}
			}

			// Decreasing is easier, we decrease the requested slider and bump
			// up the tax rate.
			if (lessScience) {
				player.scienceRate--;
				player.taxRate++;
			}

			if (lessLuxury) {
				player.luxuryRate--;
				player.taxRate++;
			}

			// Update citizen moods in all cities, as changing the sliders can
			// change moods.
			foreach (City city in player.cities) {
				city.RecalculateCitizenMoods(EngineStorage.gameData);
			}

			// Update the ui to reflect our changes.
			new MsgUpdateUiAfterDomesticChange().send();
		}
	}

	public class MsgEndTurn : MessageToEngine {
		private ILogger log = Log.ForContext<MsgEndTurn>();

		public override async void process() {
			Player controller = EngineStorage.gameData.GetPlayer(EngineStorage.uiControllerID);

			// Reorder the unit list so that non-busy units will be selected
			// first.
			controller.units.Sort((x, y) => x.IsBusy().CompareTo(y.IsBusy()));

			controller.hasPlayedThisTurn = true;
			await TurnHandling.AdvanceTurn();
		}
	}

	public class MsgPerformUnitAction : MessageToEngine {
		private MapUnit unit;
		public MsgPerformUnitAction(MapUnit unit) {
			this.unit = unit;
		}

		public override async void process() {
			await unit.PerformBusyAction();
		}
	}

	public class MsgDoHurryProduction : MessageToEngine {
		private City city;
		public MsgDoHurryProduction(City c) {
			city = c;
		}

		public override void process() {
			city.HurryProduction();
		}
	}

	public class MsgSetAnimationsEnabled : MessageToEngine {
		private bool enabled;

		public MsgSetAnimationsEnabled(bool enabled) {
			this.enabled = enabled;
		}

		public override void process() {
			EngineStorage.animationsEnabled = enabled;
		}
	}

	public class MsgToggleAnimationsEnabled : MessageToEngine {

		public MsgToggleAnimationsEnabled() {
		}

		public override void process() {
			EngineStorage.animationsEnabled = !EngineStorage.animationsEnabled;
		}
	}

	public class MsgBuildCity : MessageToEngine {
		private MapUnit unit;
		private string name;

		public MsgBuildCity(MapUnit unit, string name) {
			this.unit = unit;
			this.name = name;
		}

		public override async void process() {
			City? city = await unit.buildCity(name);
			if (city != null) {
				new MsgCityCreated(city).send();
			}
		}
	}

	public class MsgDiplomacyCompleted : MessageToEngine {
		public override void process() { }
	}
}
