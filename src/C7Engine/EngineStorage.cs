namespace C7Engine {
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
	public static class EngineStorage {
		public static Mutex gameDataMutex = new Mutex();
		internal static GameData gameData { get; set; }
		public static ID uiControllerID;
		internal static bool animationsEnabled = true;

		private static Thread engineThread = null;
		internal static AutoResetEvent uiEvent = new AutoResetEvent(false); // Used to block engineThread while waiting for the UI, f.e. while
																			// an animation plays.

		internal static ConcurrentQueue<MessageToEngine> pendingMessages = new ConcurrentQueue<MessageToEngine>();
		internal static AutoResetEvent actionAddedToQueue = new AutoResetEvent(false);

		public static ConcurrentQueue<MessageToUI> messagesToUI = new ConcurrentQueue<MessageToUI>();

		private static void processActions() {
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

		internal static void createThread() {
			// TODO: What if engineThread is not null, i.e. if the thread has already been created? Should we join() it? Does it matter?
			engineThread = new Thread(processActions);
			engineThread.Start();
		}
	}

	public class UIGameDataAccess : IDisposable {
		public UIGameDataAccess() {
			EngineStorage.gameDataMutex.WaitOne();
		}

		public void Dispose() {
			EngineStorage.gameDataMutex.ReleaseMutex();
		}

		public GameData gameData {
			get {
				return EngineStorage.gameData;
			}
		}
	}
}
