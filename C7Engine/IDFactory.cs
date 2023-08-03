using System.Collections.Generic;
using C7GameData;

namespace C7Engine {

	public class IDFactory {
		private Dictionary<string, int> keyCounter;

		// TODO: when creating an IDFactory for a loaded save game, take care
		// to find the largest n-value for each ID key.
		// ie. loading a save with units ["warrior-1", "warrior-3", "worker-2"]
		//     should result in an id factory with
		// keyCounter = {
		//     "warrior": 3,
		//     "worker": 2,
		// }
		public IDFactory() {
			keyCounter = new Dictionary<string, int>();
		}

		public ID CreateID(string key) {
			int n = keyCounter.GetValueOrDefault(key, 1);
			keyCounter[key] = n + 1;
			return new ID(key, n);
		}
	}
}
