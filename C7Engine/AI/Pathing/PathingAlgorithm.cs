using C7GameData;

namespace C7Engine.Pathing
{
	public interface PathingAlgorithm
	{
		TilePath PathFrom(Tile start, Tile destination);
	}
}
