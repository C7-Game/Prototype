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
		public override void process()
		{
			Console.WriteLine("Engine received shutdown message.");
		}
	}

	public class MsgSetFortification : MessageToEngine
	{
		private Guid unitGUID;
		private bool fortifyElseWake;

		public MsgSetFortification(Guid unitGUID, bool fortifyElseWake)
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
		private Guid unitGUID;
		private TileDirection dir;

		public MsgMoveUnit(Guid unitGUID, TileDirection dir)
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
		private Guid unitGUID;
		private int destX;
		private int destY;

		public MsgSetUnitPath(Guid unitGUID, Tile tile)
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
		private Guid unitGUID;

		public MsgSkipUnitTurn(Guid unitGUID)
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
		private Guid unitGUID;

		public MsgDisbandUnit(Guid unitGUID)
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
		private Guid unitGUID;
		private string cityName;

		public MsgBuildCity(Guid unitGUID, string cityName)
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

	public class MsgChooseProduction : MessageToEngine {
		private Guid cityGUID;
		private string producibleName;

		public MsgChooseProduction(Guid cityGUID, string producibleName)
		{
			this.cityGUID = cityGUID;
			this.producibleName = producibleName;
		}

		public override void process()
		{
			City city = EngineStorage.gameData.GetCity(cityGUID);
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
		public override void process()
		{
			Player controller = EngineStorage.gameData.players.Find(p => p.guid == EngineStorage.uiControllerID);

			foreach (MapUnit unit in controller.units) {
				Console.WriteLine($"{unit}, path length: {unit.path?.PathLength() ?? 0}");
				if (unit.path?.PathLength() > 0) {
					unit.moveAlongPath();
				}
			}

			controller.hasPlayedThisTurn = true;
			TurnHandling.AdvanceTurn();
		}
	}
}
