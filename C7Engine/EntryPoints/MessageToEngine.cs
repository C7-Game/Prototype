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

public class MsgFortifyUnit : MessageToEngine
{
	private string unitGUID;

	public MsgFortifyUnit(string unitGUID)
	{
		this.unitGUID = unitGUID;
	}

	public override void process()
	{
		MapUnit unit = EngineStorage.gameData.mapUnits.Find(u => u.guid == unitGUID);

		// Simply do nothing if we weren't given a valid GUID. TODO: Maybe this is an error we need to handle? In an MP game, we should reject
		// invalid actions at the server level but at the client level an invalid action received from the server indicates a desync.
		if (unit != null)
			unit.fortify();
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
		MapUnit unit = EngineStorage.gameData.mapUnits.Find(u => u.guid == unitGUID);
		if (unit != null)
			unit.move(dir);
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
		MapUnit unit = EngineStorage.gameData.mapUnits.Find(u => u.guid == unitGUID);
		if (unit != null)
			unit.skipTurn();
	}
}

}
