using System.IO;
using System.Linq;
using ConvertCiv3Media;
using EngineTests.Utils;
using QueryCiv3;
using Xunit;

namespace EngineTests.GameData;

public class AmbReaderTest {

	[SkippableFact]
	public void WorkerRunAmbTest() {
		Skip.If(Civ3TestData.ShouldSkipCiv3DependentTests(), "No Civ3 install found.");

		string path = Path.Combine(Civ3Location.GetCiv3Path(), "Art", "Units", "Worker", "WorkerRun.amb");

		Sfx sfx1 = new Amb(path).soundEffects.First();
		Sfx sfx2 = new Amb(path).soundEffects.Skip(1).First();

		Assert.Equal("WorkRunFoot1.wav", sfx1.wavName);
		Assert.Equal("WorkRunFoot2.wav", sfx2.wavName);
	}
}
