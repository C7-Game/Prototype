using System;
using C7Engine;
using Godot;

// This node dequeues messages from the engine and passes them to the UI.
// To receive new engine messages other nodes should subscribe to the `messageConsumed` event.
public partial class MessageConsumer : Node {
	public event Action<MessageToUI> messageConsumed;

	public override void _Process(double delta) {
		if (EngineStorage.TryDequeueNextMessageToUI(out MessageToUI msg))
			messageConsumed?.Invoke(msg);
	}
}
