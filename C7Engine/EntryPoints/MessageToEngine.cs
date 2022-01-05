namespace C7Engine
{
using System;

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
}
