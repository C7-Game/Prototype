namespace C7Engine
{

using System.Linq;
using System.Collections.Generic;
using C7GameData;

public static class CityExtensions {
	public static IEnumerable<IProducible> ListProductionOptions(this City city)
	{
		return EngineStorage.gameData.unitPrototypes.Values.Where(u => city.CanBuildUnit(u));
	}
}

}
