using System;
using System.IO;
using System.Linq;
using QueryCiv3;

namespace EngineTests.Utils;

public static class Civ3TestData {
	private static readonly string[] DefaultRequiredFiles = {
		"Conquests/conquests.biq",
		"Conquests/Text/PediaIcons.txt",
	};

	public static bool ShouldSkipCiv3DependentTests() {
		// GitHub Actions sets CI, and Civ3 is not installed there. Local
		// contributors can also run without Civ3 assets or CIV3_HOME configured.
		if (Environment.GetEnvironmentVariable("CI") != null) {
			return true;
		}

		return DefaultRequiredFiles.Any(relativePath => !File.Exists(GetCiv3Path(relativePath)));
	}

	private static string GetCiv3Path(string relativePath) {
		string normalizedPath = relativePath
			.Replace('\\', Path.DirectorySeparatorChar)
			.Replace('/', Path.DirectorySeparatorChar);

		return Path.Combine(Civ3Location.GetCiv3Path(), normalizedPath);
	}
}
