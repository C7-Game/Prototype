using System.IO;
using IniParser.Exceptions;

namespace C7Engine {
	using IniParser;
	using IniParser.Model;

	public class C7Settings {
		private const string SETTINGS_FILE_NAME = "C7.ini";
		public static IniData settings;

		public static void LoadSettings() {
			try {
				settings = new FileIniDataParser().ReadFile(SETTINGS_FILE_NAME);
			} catch (ParsingException) {
				//First run.  The file doesn't exist.  That's okay.  We'll use sensible defaults.
				settings = new IniData();
				SaveSettings();
			}
		}

		public static void SaveSettings() {
			new FileIniDataParser().WriteFile(SETTINGS_FILE_NAME, settings);
		}

		public static void SetValue(string section, string key, string value) {
			if (settings == null) {
				LoadSettings();
			}
			settings[section][key] = value;
		}

		public static string GetSettingValue(string section, string key) {
			if (settings == null) {
				LoadSettings();
			}
			return settings[section][key];
		}

		public static string GetSettingsValueOrDefault(string section, string key, string defaultValue) {
			if (settings == null) {
				LoadSettings();
			}
			if (settings[section] == null) {
				return defaultValue;
			}
			if (settings[section][key] == null) {
				return defaultValue;
			}
			return settings[section][key];
		}

		public static bool UseStandaloneMode() {
			return GetSettingsValueOrDefault("locations", "useStandaloneMode", "false") == "true";
		}
	}
}
