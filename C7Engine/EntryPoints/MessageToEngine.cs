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
		private EntityId unitId;
		private bool fortifyElseWake;

		public MsgSetFortification(EntityId unitId, bool fortifyElseWake)
		{
			this.unitId = unitId;
			this.fortifyElseWake = fortifyElseWake;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitId);

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
		private EntityId unitId;
		private TileDirection dir;

		public MsgMoveUnit(EntityId unitId, TileDirection dir)
		{
			this.unitId = unitId;
			this.dir = dir;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitId);
			unit?.move(dir);
		}
	}

	public class MsgSetUnitPath : MessageToEngine
	{
		private EntityId unitId;
		private int destX;
		private int destY;

		public MsgSetUnitPath(EntityId unitId, Tile tile)
		{
			this.unitId = unitId;
			this.destX = tile.xCoordinate;
			this.destY = tile.yCoordinate;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitId);
			unit?.setUnitPath(EngineStorage.gameData.map.tileAt(destX, destY));
		}
	}

	public class MsgSkipUnitTurn : MessageToEngine
	{
		private EntityId unitId;

		public MsgSkipUnitTurn(EntityId unitId)
		{
			this.unitId = unitId;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitId);
			unit?.skipTurn();
		}
	}

	public class MsgDisbandUnit : MessageToEngine {
		private EntityId unitId;

		public MsgDisbandUnit(EntityId unitId)
		{
			this.unitId = unitId;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitId);
			unit?.disband();
		}
	}

	public class MsgBuildCity : MessageToEngine {
		private EntityId unitId;
		private string cityName;

		public MsgBuildCity(EntityId unitId, string cityName)
		{
			this.unitId = unitId;
			this.cityName = cityName;
		}

		public override void process()
		{
			MapUnit unit = EngineStorage.gameData.GetUnit(unitId);
			unit?.buildCity(cityName);
		}
	}

	public class MsgBuildRoad : MessageToEngine {
		private EntityId unitId;

		public MsgBuildRoad(EntityId unitId) {
			this.unitId = unitId;
		}

		public override void process() {
			MapUnit unit = EngineStorage.gameData.GetUnit(unitId);
			unit?.buildRoad();
		}
	}

	public class MsgChooseProduction : MessageToEngine {
		private string cityGUID;
		private string producibleName;

		public MsgChooseProduction(string cityGUID, string producibleName)
		{
			this.cityGUID = cityGUID;
			this.producibleName = producibleName;
		}

		public override void process()
		{
			City city = EngineStorage.gameData.cities.Find(c => c.guid == cityGUID);
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

	public class MsgSaveGame : MessageToEngine {
		private string path;

		public MsgSaveGame(string path) {
			this.path = path;
		}

		public override void process() {
			SaveManager.Save(this.path);
		}
	}
}
