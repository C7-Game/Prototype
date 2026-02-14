using System;
using C7Engine;
using Godot;

public partial class MessageConsumer : Node {
	public event Action<MessageToUI> messageConsumed;

	public override void _Process(double delta) {
		if (EngineStorage.TryDequeueNextMessageToUI(out MessageToUI msg))
			messageConsumed?.Invoke(msg);
	}
}
