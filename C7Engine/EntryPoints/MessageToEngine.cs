namespace C7Engine
{
using System;

public abstract class MessageToEngine {
	public abstract void process();
}

public class ShutdownEngine : MessageToEngine {
	public override void process()
	{
		Console.WriteLine("Engine received shutdown message.");
	}
}
}
