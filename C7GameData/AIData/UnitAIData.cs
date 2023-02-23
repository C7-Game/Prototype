using System.Text.Json.Serialization;

namespace C7GameData.AIData
{
	//Base class from which other AIs can inherit.
	[JsonDerivedType(typeof(CombatAIData), typeDiscriminator: nameof(CombatAIData))]
	[JsonDerivedType(typeof(DefenderAIData), typeDiscriminator: nameof(DefenderAIData))]
	[JsonDerivedType(typeof(ExplorerAIData), typeDiscriminator: nameof(ExplorerAIData))]
	[JsonDerivedType(typeof(SettlerAIData), typeDiscriminator: nameof(SettlerAIData))]
	public interface UnitAIData
	{

	}
}
