using C7GameData;
using Newtonsoft.Json;
using Xunit;

namespace C7GameDataTests {
	public class UnitPrototypeTest {
		[Fact]
		public void UnitPrototypeCanBeSerializedProperly() {
			UnitPrototype prototype = new UnitPrototype();
			prototype.name = "Frigate";
			prototype.shieldCost = 70;
			prototype.populationCost = 0;
			prototype.attack = 4;
			prototype.defense = 4;
			prototype.bombard = 1;
			prototype.movement = 5;
			prototype.iconIndex = 72;
			prototype.categories.Add("Sea");
			prototype.actions.Add("Move");
			prototype.attributes.Add("Can move on Sea");

			string serialized = JsonConvert.SerializeObject(prototype, Formatting.Indented);

			UnitPrototype restored = JsonConvert.DeserializeObject<UnitPrototype>(serialized);

			// Todo: Figure out how to override toEquals conveniently in C# like in Java.
			// Rider has an auto-gen capability but it's not quite the same...
			Assert.Equal(prototype.categories, restored.categories);
			Assert.Equal(prototype.actions, restored.actions);
		}
	}
}
