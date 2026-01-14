namespace C7GameData.AIData {
	public class ExplorerAIData : UnitAIData {
		public Tile destination;
		public TilePath pathToDestination;

		public override string ToString() {
			return "exploring toward " + destination;
		}
	}
}
