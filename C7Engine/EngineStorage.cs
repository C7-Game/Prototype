namespace C7Engine
{
	using System;
	using System.Threading;
	using System.Collections.Concurrent;
	using C7GameData;

	/**
	 * This class stores references to data that the engine needs between calls from the player.
	 * Most obviously this includes a reference to the C7GameData, but it might eventually
	 * also include things like keeping track of which networked players are up to date.
	 *
	 * Note that we should NOT store pointers to pieces of the game data here; that will
	 * all be handled within C7GameData.  We just need a pointer to the main, top level
	 * so we don't forget the state of the game after we create it.
	 **/
	public static class EngineStorage
	{
		public static Mutex gameDataMutex = new Mutex();
		internal static GameData gameData {get; set;}

		private static Thread engineThread;
		internal static ConcurrentQueue<MessageToEngine> pendingMessages = new ConcurrentQueue<MessageToEngine>();
		internal static AutoResetEvent actionAddedToQueue = new AutoResetEvent(false);

		public static ConcurrentQueue<MessageToUI> messagesToUI = new ConcurrentQueue<MessageToUI>();

		public static IAnimationControl animTracker; // Must be set by the UI during initialization

		private static void processActions()
		{
			bool stopProcessing = false;
			while (!stopProcessing) {
				actionAddedToQueue.WaitOne();
				MessageToEngine msg;
				gameDataMutex.WaitOne();
				while (pendingMessages.TryDequeue(out msg)) {
					msg.process();
					if (msg is MsgShutdownEngine) {
						stopProcessing = true;
						break;
					}
				}
				gameDataMutex.ReleaseMutex();
			}
		}

		// The UI must call this function to set up the engine. Right now all it does it set animTracker and spawn the engine thread. There is
		// no deinitialize function b/c the way to deinit the engine is to pass it a ShutdownEngine message.
		// TODO: This initialize function and the thread, message queue, and AutoResetEvent should be moved somewhere other than
		// EngineStorage. I'm just dropping them here for now since this is the closest thing we have to a root Engine class.
		public static void initialize(IAnimationControl _animTracker)
		{
			animTracker = _animTracker;

			// TODO: If engineThread is already created and running, stop it first
			engineThread = new Thread(processActions);
			engineThread.Start();
		}

		/**
		 * Updates the game data pointer to a new set of game data.
		 * This may be a randomly generated map, or data loaded from a scenario
		 * or from a save file.
		 * The engine will no longer have a reference to the old game data.
		 **/
		public static void setGameData(GameData newGameData)
		{
			gameData = newGameData;
		}
	}

	public class UIGameDataAccess : IDisposable {
		public UIGameDataAccess()
		{
			EngineStorage.gameDataMutex.WaitOne();
		}

		public void Dispose()
		{
			EngineStorage.gameDataMutex.ReleaseMutex();
		}

		public GameData gameData {
			get {
				return EngineStorage.gameData;
			}
		}
	}
}
