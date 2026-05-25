using System.Formats.Tar;
using System.IO;
using System.Text.Json;

namespace EngineTests.Utils;

public class JsonUtils {
	public static JsonDocument LoadBaseRuleset() {
		var rulesPath = Path.Combine(PathUtils.GameModesDir, "civ3", "ruleset.json");
		return JsonDocument.Parse(File.ReadAllText(rulesPath));
	}
}
