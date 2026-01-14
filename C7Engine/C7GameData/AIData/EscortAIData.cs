namespace C7GameData.AIData {
	public class EscortAIData : UnitAIData {
		public MapUnit unitToEscort;

		public override string ToString() {
			return "escorting " + unitToEscort;
		}
	}
}
