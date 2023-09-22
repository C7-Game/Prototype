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

		public string getMixedNumber()
		{
			// Modified from Stack overflow https://stackoverflow.com/a/32903747
			// Only supports positive values
			double absolute_accuracy=0.01;
			double val = remaining;
			if(val < 0)
			{
				return "0";
			}

			int whole_num = (int) Math.Floor(val);
			val -= whole_num;


			if(val <= absolute_accuracy)
			{
				return whole_num.ToString();
			}

			// The lower fraction is 0/1
			int lower_n = 0;
			int lower_d = 1;

			// The upper fraction is 1/1
			// Start with an upper guess of 1 and a lower guess of 0, then converge through a binary search
			int upper_n = 1;
			int upper_d = 1;
			while (true)
			{
				// The middle fraction is (lower_n + upper_n) / (lower_d + upper_d)
				int middle_n = lower_n + upper_n;
				int middle_d = lower_d + upper_d;

				if (middle_d * (val + absolute_accuracy) < middle_n)
				{
					// real + error < middle : middle is our new upper
					Seek(ref upper_n, ref upper_d, lower_n, lower_d, (un, ud) => (lower_d + ud) * (val + absolute_accuracy) < (lower_n + un));
				}
				else if (middle_n < (val - absolute_accuracy) * middle_d)
				{
					// middle < real - error : middle is our new lower
					// middle < real - error : middle is our new lower
					Seek(ref lower_n, ref lower_d, upper_n, upper_d, (ln, ld) => (ln + upper_n) < (val - absolute_accuracy) * (ld + upper_d));
				}
				else
				{
					// Middle is our best fraction
					if(whole_num == 0)
					{
						return "(" + middle_n.ToString() + "/" + middle_d.ToString() + ")";
					}
					return "(" + whole_num.ToString() + " " + middle_n.ToString() + "/" + middle_d.ToString() + ")";
				}
			}

		}

		/// Binary seek for the value where f() becomes false.
		/// Used for mixed number calculation. Taken from https://stackoverflow.com/a/32903747
		void Seek(ref int a, ref int b, int ainc, int binc, Func<int, int, bool> f)
		{
			a += ainc;
			b += binc;

			if (f(a, b))
			{
				int weight = 1;

				do
				{
					weight *= 2;
					a += ainc * weight;
					b += binc * weight;
				}
				while (f(a, b));

				do
				{
					weight /= 2;

					int adec = ainc * weight;
					int bdec = binc * weight;

					if (!f(a - adec, b - bdec))
					{
						a -= adec;
						b -= bdec;
					}
				}
				while (weight > 1);
			}
		}

	}
}
