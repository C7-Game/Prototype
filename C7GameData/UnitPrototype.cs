using System.Collections.Generic;

namespace C7GameData
{
	using System;

	/**
	 * The prototype for a unit, which defines the characteristics of a unit.
	 * For example, a Spearman might have 1 attack, 2 defense, and 1 movement.
	 **/
	public class UnitPrototype : IProducible
	{
		public string name { get; set; }
		public int shieldCost { get; set; }
		public int populationCost { get; set; }
		public int attack {get; set;}
		public int defense {get; set;}
		public int bombard {get; set;}
		public int movement {get; set;}
		public int iconIndex {get; set;}

		public HashSet<string> categories = new HashSet<string>();

		public HashSet<string> actions = new HashSet<string>();

		public HashSet<string> attributes = new HashSet<string>();

		public override string ToString()
		{
			return $"{name} ({attack}/{defense}/{movement})";
		}

		public double BaseStrength(CombatRole role)
		{
			switch (role) {
			case CombatRole.Attack:                  return attack;
			case CombatRole.Defense:                 return defense;
			case CombatRole.Bombard:                 return bombard;
			case CombatRole.BombardDefense:          return defense;
			case CombatRole.DefensiveBombard:        return bombard;
			case CombatRole.DefensiveBombardDefense: return defense;
			default: throw new ArgumentOutOfRangeException("Invalid CombatRole");
			}
		}

		public MapUnit GetInstance()
		{
			MapUnit instance = new MapUnit();
			instance.unitType = this;
			instance.hitPointsRemaining = 3;    //todo: make this configurable
			instance.movementPointsRemaining = movement;
			return instance;
		}
	}
}
