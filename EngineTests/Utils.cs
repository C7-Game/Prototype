namespace EngineTests {

	using System.IO;
	using System.Runtime.CompilerServices;

	static class Utils {

		public static string DataPath;

		private static string ThisFilePath([CallerFilePath] string path = null) {
			return path;
		}

		static Utils() {
			DataPath = Path.Combine(Path.GetDirectoryName(ThisFilePath()), "data/");
		}

	}

}
