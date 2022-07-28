namespace C7Engine.Pathing
{
	/**
	 * Returns a pathing algorithm to use.
	 * Eventually, this will depend on some map considerations.
	 * For now, just return the first one.
	 */
	public class PathingAlgorithmChooser
	{
		private static PathingAlgorithm theAlgorithm = new DijkstrasAlgorithm(new WalkerOnLand());

		public static PathingAlgorithm GetAlgorithm()
		{
			return theAlgorithm;
		}
	}
}
