using Serilog;

namespace C7Engine {
	using System;
	using C7GameData;

	public abstract class MessageToEngine {
		public abstract void process();

		public void send() {
			EngineStorage.pendingMessages.Enqueue(this);
			EngineStorage.actionAddedToQueue.Set();
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

		public override void process() {
			MapUnit unit = EngineStorage.gameData.GetUnit(unitID);
			unit?.move(dir);

			// The unit moved to a new tile - if it still has movement points,
			// update the UI to reflect this new position and movement points.
			if (unit?.movementPoints.canMove == true) {
				new MsgUpdateUiAfterMove().send();
			}
		}
	}

	public class MsgSetUnitPath : MessageToEngine {
		private ID unitID;
		private int destX;
		private int destY;

		public MsgSetUnitPath(ID unitID, Tile tile) {
			this.unitID = unitID;
			this.destX = tile.XCoordinate;
			this.destY = tile.YCoordinate;
		}

		public override void process() {
			MapUnit unit = EngineStorage.gameData.GetUnit(unitID);
			unit?.setUnitPath(EngineStorage.gameData.map.tileAt(destX, destY));

			// The unit moved to a new tile - if it still has movement points,
			// update the UI to reflect this new position and movement points.
			if (unit?.movementPoints.canMove == true) {
				new MsgUpdateUiAfterMove().send();
			}
		}
	}

	// A generic class that allows the UI to have the game engine run some
	// action, assumed to be on a unit.
	//
	// Actions that require more than a 1 or 2 line lambda should probably use
	// a custom subclass.
	public class ActionToEngineMsg : MessageToEngine {
		private Action action;
		public ActionToEngineMsg(Action action) {
			this.action = action;
		}

		public override void process() {
			action();
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
				foreach (IProducible producible in city.ListProductionOptions()) {
					if (producible.name == producibleName) {
						city.SetItemBeingProduced(producible);
						break;
					}
				}
			}
		}
	}

	public class MsgChooseResearch : MessageToEngine {
		private ID techId;
		public MsgChooseResearch(ID techId) {
			this.techId = techId;
		}

		public override void process() {
			Player player = EngineStorage.gameData.GetHumanPlayers()[0];
			if (player.currentlyResearchedTech == techId) {
				return;
			}
			Tech requestedTech = EngineStorage.gameData.techs.Find(t => t.id == techId);

			// Ensure this is an eligible tech to research.
			//
			// TODO: do a topological sort to allow a queue of techs to study.
			foreach (Tech prereq in requestedTech.Prerequisites) {
				if (!player.knownTechs.Contains(prereq.id)) {
					return;
				}
			}

			// Start researching this tech and update the UI.
			player.SetCurrentlyResearchedTech(requestedTech.id);
			new MsgUpdateUiAfterTechSelection().send();
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
			Player player = EngineStorage.gameData.GetHumanPlayers()[0];

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

			// Update the ui to reflect our changes.
			new MsgUpdateUiAfterSliderChange().send();
		}
	}

	public class MsgEndTurn : MessageToEngine {

		private ILogger log = Log.ForContext<MsgEndTurn>();

		public override void process() {
			Player controller = EngineStorage.gameData.GetPlayer(EngineStorage.uiControllerID);

			foreach (MapUnit unit in controller.units) {
				log.Debug($"{unit}, path length: {unit.path?.PathLength() ?? 0}");
				if (unit.path?.PathLength() > 0) {
					unit.moveAlongPath();
				}
			}

			controller.hasPlayedThisTurn = true;
			TurnHandling.AdvanceTurn();
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
}
