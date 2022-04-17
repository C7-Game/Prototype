namespace C7Engine
{
	using System.Threading;
	using C7GameData;

	public class MessageToUI {
		public void send()
		{
			EngineStorage.messagesToUI.Enqueue(this);
		}
	}

	public class MsgStartUnitAnimation : MessageToUI {
		public string unitGUID;
		public MapUnit.AnimatedAction action;
		public AutoResetEvent completionEvent;
		public AnimationEnding ending;

		public MsgStartUnitAnimation(MapUnit unit, MapUnit.AnimatedAction action, AutoResetEvent completionEvent, AnimationEnding ending)
		{
			this.unitGUID = unit.guid;
			this.action = action;
			this.completionEvent = completionEvent;
			this.ending = ending;
		}
	}

	public class MsgStartEffectAnimation : MessageToUI {
		public int tileIndex;
		public AnimatedEffect effect;
		public AutoResetEvent completionEvent;
		public AnimationEnding ending;

		public MsgStartEffectAnimation(Tile tile, AnimatedEffect effect, AutoResetEvent completionEvent, AnimationEnding ending)
		{
			this.tileIndex = EngineStorage.gameData.map.tileCoordsToIndex(tile.xCoordinate, tile.yCoordinate);
			this.effect = effect;
			this.completionEvent = completionEvent;
			this.ending = ending;
		}
	}

	public class MsgStartTurn : MessageToUI {}

	public class MsgFinishedSave : MessageToUI {}

}
