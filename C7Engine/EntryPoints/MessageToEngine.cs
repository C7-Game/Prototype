using Serilog;

namespace C7Engine
{
	using System;
	using C7GameData;

	public abstract class MessageToEngine {
		public abstract void process();

		public void send()
		{
			EngineStorage.pendingMessages.Enqueue(this);
			EngineStorage.actionAddedToQueue.Set();
		}
	}

	public class MsgShutdownEngine : MessageToEngine {
		private ILogger log = Log.ForContext<MsgShutdownEngine>();

		public override void process()
		{
			log.Information("Engine received shutdown message.");
		}
	}

	public class MsgSetFortification : MessageToEngine
	{
		private string unitGUID;
		private bool fortifyElseWake;

		public MsgSetFortification(string unitGUID, bool fortifyElseWake)
		{
			this.unitGUID = unitGUID;
			this.fortifyElseWake = fortifyElseWake;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitGUID);

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

	public class MsgMoveUnit : MessageToEngine
	{
		private string unitGUID;
		private TileDirection dir;

		public MsgMoveUnit(string unitGUID, TileDirection dir)
		{
			this.unitGUID = unitGUID;
			this.dir = dir;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitGUID);
			unit?.move(dir);
		}
	}

	public class MsgSetUnitPath : MessageToEngine
	{
		private string unitGUID;
		private int destX;
		private int destY;

		public MsgSetUnitPath(string unitGUID, Tile tile)
		{
			this.unitGUID = unitGUID;
			this.destX = tile.xCoordinate;
			this.destY = tile.yCoordinate;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitGUID);
			unit?.setUnitPath(EngineStorage.gameData.map.tileAt(destX, destY));
		}
	}

	public class MsgSkipUnitTurn : MessageToEngine
	{
		private string unitGUID;

		public MsgSkipUnitTurn(string unitGUID)
		{
			this.unitGUID = unitGUID;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitGUID);
			unit?.skipTurn();
		}
	}

	public class MsgDisbandUnit : MessageToEngine {
		private string unitGUID;

		public MsgDisbandUnit(string unitGUID)
		{
			this.unitGUID = unitGUID;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitGUID);
			unit?.disband();
		}
	}

	public class MsgBuildCity : MessageToEngine {
		private string unitGUID;
		private string cityName;

		public MsgBuildCity(string unitGUID, string cityName)
		{
			this.unitGUID = unitGUID;
			this.cityName = cityName;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitGUID);
			unit?.buildCity(cityName);
		}
	}

	public class MsgBuildRoad : MessageToEngine {
		private string unitGUID;

		public MsgBuildRoad(string unitGUID) {
			this.unitGUID = unitGUID;
		}

		public override void process() {
			MapUnit unit = EngineStorage.gameData.GetUnit(unitGUID);
			unit?.buildRoad();
		}
	}

	public class MsgChooseProduction : MessageToEngine {
		private ID cityID;
		private string producibleName;

		public MsgChooseProduction(ID cityID, string producibleName)
		{
			this.cityID = cityID;
			this.producibleName = producibleName;
		}

		public override void process()
		{
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

	public class MsgEndTurn : MessageToEngine {

		private ILogger log = Log.ForContext<MsgEndTurn>();

		public override void process()
		{
			Player controller = EngineStorage.gameData.players.Find(p => p.guid == EngineStorage.uiControllerID);

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

		public MsgSetAnimationsEnabled(bool enabled)
		{
			this.enabled = enabled;
		}

		public override void process()
		{
			EngineStorage.animationsEnabled = enabled;
		}
	}
}
