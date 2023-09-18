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
		private ID unitID;
		private bool fortifyElseWake;

		public MsgSetFortification(ID unitID, bool fortifyElseWake)
		{
			this.unitID = unitID;
			this.fortifyElseWake = fortifyElseWake;
		}

		public override void process()
		{
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

	public class MsgMoveUnit : MessageToEngine
	{
		private ID unitID;
		private TileDirection dir;

		public MsgMoveUnit(ID unitID, TileDirection dir)
		{
			this.unitID = unitID;
			this.dir = dir;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitID);
			unit?.move(dir);
		}
	}

	public class MsgSetUnitPath : MessageToEngine
	{
		private ID unitID;
		private int destX;
		private int destY;

		public MsgSetUnitPath(ID unitID, Tile tile)
		{
			this.unitID = unitID;
			this.destX = tile.xCoordinate;
			this.destY = tile.yCoordinate;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitID);
			unit?.setUnitPath(EngineStorage.gameData.map.tileAt(destX, destY));
		}
	}

	public class MsgSkipUnitTurn : MessageToEngine
	{
		private ID unitID;

		public MsgSkipUnitTurn(ID unitID)
		{
			this.unitID = unitID;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitID);
			unit?.skipTurn();
		}
	}

	public class MsgDisbandUnit : MessageToEngine {
		private ID unitID;

		public MsgDisbandUnit(ID unitID)
		{
			this.unitID = unitID;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitID);
			unit?.disband();
		}
	}

	public class MsgBuildCity : MessageToEngine {
		private ID unitID;
		private string cityName;

		public MsgBuildCity(ID unitID, string cityName)
		{
			this.unitID = unitID;
			this.cityName = cityName;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitID);
			unit?.buildCity(cityName);
		}
	}

	public class MsgBuildRoad : MessageToEngine {
		private ID unitID;

		public MsgBuildRoad(ID unitID) {
			this.unitID = unitID;
		}

		public override void process() {
			MapUnit unit = EngineStorage.gameData.GetUnit(unitID);
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
