using System.Linq;
using System.Runtime.InteropServices;

namespace QueryCiv3 {
	public class Civ3Location {
		public static readonly string RegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Infogrames Interactive\Civilization III";

		static private string SteamCommonDir() {
			string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
			return home == null ? null : System.IO.Path.Combine(home, "Library/Application Support/Steam/steamapps/common");
		}

		static private bool FolderIsCiv3(System.IO.DirectoryInfo di) {
			return di.EnumerateFiles().Any(f => f.Name == "civ3id.mb");
		}

		static public string Civ3PathFromRegistry() {
			// Assuming 64-bit platform, get vanilla Civ3 install folder from registry
			// Return null if value not present or if key not found
			object path = Microsoft.Win32.Registry.GetValue(RegistryKey, "install_path", null);
			return path == null ? null : (string)path;
		}

		static public string GetCiv3Path() {
			// Use CIV3_HOME env var if present
			string path = System.Environment.GetEnvironmentVariable("CIV3_HOME");
			if (path != null) { return path; }

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				// Look up in Windows registry if present
				path = Civ3PathFromRegistry();
				if (path != null) { return path; }
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				// Check for a civ3 folder in steamapps/common
				System.IO.DirectoryInfo root = new System.IO.DirectoryInfo(Civ3Location.SteamCommonDir());
				foreach (System.IO.DirectoryInfo di in root.GetDirectories()) {
					if (Civ3Location.FolderIsCiv3(di)) {
						return di.FullName;
					}
				}
			}
			return "/civ3/path/not/found";
		}
	}
}
