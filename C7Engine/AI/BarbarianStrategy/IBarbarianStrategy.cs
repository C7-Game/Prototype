using System.Threading.Tasks;
using C7GameData;

namespace C7Engine;

internal interface IBarbarianStrategy {
	Task PlayUnitTurn(Player player, MapUnit unit);
}
