using System;
using C7GameData;
using Xunit;

public class SaveTest
{
	[Fact]
	public void CanLoadSaveTest()
	{
		Console.Out.WriteLine("loading");
		var save = C7SaveFormat.Load("../../../../C7/Text/c7-static-map-save-2.json");
		save.PostLoadProcess();
		Console.Out.WriteLine("version is " + save.Version);
	}
}
