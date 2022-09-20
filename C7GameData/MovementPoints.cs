using System;

namespace C7GameData
{
	/**
	 * Struct for representing unit movement points values, such as 2, 1/3, 1/10 etc
	 * epsilon required for avoiding rounding errors
	 */
	public struct MovementPoints {
		private const float epsilon = 0.0001f;

		public float remaining { get; private set; }

		public bool canMove { get => remaining > epsilon; }

		public void onUnitMove(float v) {
			remaining = Math.Max(0.0f, remaining - v);
		}

		public void onConsumeAll() {
			remaining = 0.0f;
		}

		public void reset(float maxMovementPoints) {
			remaining = maxMovementPoints;
		}

		/**
		* I'd like to enhance this so it's like Civ4, where the skip turn action takes the unit out of the rotation, but you can change your
		* mind if need be.  But for now it'll be like Civ3, where you're out of luck if you realize that unit was needed for something; that
		* also simplifies things here.
		**/
		public void skipTurn() {
			remaining = 0.0f;
		}
	}
}
