namespace C7Engine
{
using System;
using System.Threading;
using C7GameData;

public class MessageToUI {
	public void send()
	{
		EngineStorage.messagesToUI.Enqueue(this);
	}
}

public class MsgStartAnimation : MessageToUI {
	public string unitGUID;
	public MapUnit.AnimatedAction action;
	public AutoResetEvent completionEvent;

	public MsgStartAnimation(MapUnit unit, MapUnit.AnimatedAction action, AutoResetEvent completionEvent)
	{
		this.unitGUID = unit.guid;
		this.action = action;
		this.completionEvent = completionEvent;
	}
}

public class MsgStartTurn : MessageToUI {}
}
