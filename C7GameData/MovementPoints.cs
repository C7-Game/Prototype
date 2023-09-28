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
			// Might need to mess with accuracy; I want to avoid a movement of 1 resulting in 99/100
			double absoluteAccuracy=0.005;
			double val = remaining;
			if(val < 0)
			{
				return "0";
			}

			int wholeNum = (int) Math.Round(val,0);
			

			// This is important for important cases like 0.999. We have to round (possibly up) and then check if the whole number is close enough. Otherwise, we'll floor and create a mixed number 
			if(Math.Abs(wholeNum - val) <= absoluteAccuracy)
			{
				return wholeNum.ToString();
			}
			
			wholeNum = (int) Math.Floor(val);
			val -= wholeNum;
			if(val <= absoluteAccuracy)
			{
				return wholeNum.ToString();
			}

			// The lower fraction is 0/1
			int lowerN = 0;
			int lowerD = 1;

			// The upper fraction is 1/1
			// Start with an upper guess of 1 and a lower guess of 0, then converge through a binary search
			int upperN = 1;
			int upperD = 1;
			while (true)
			{
				// The middle fraction is (lowerN + upperN) / (lowerD + upperD)
				int middleN = lowerN + upperN;
				int middleD = lowerD + upperD;

				if (middleD * (val + absoluteAccuracy) < middleN)
				{
					// real + error < middle : middle is our new upper
					(upperD, upperD) = Seek(upperN, upperD, lowerN, lowerD, (un, ud) => (lowerD + ud) * (val + absoluteAccuracy) < (lowerN + un));
				}
				else if (middleN < (val - absoluteAccuracy) * middleD)
				{
					// middle < real - error : middle is our new lower
					// middle < real - error : middle is our new lower
					(lowerN, lowerD) = Seek(lowerN, lowerD, upperN, upperD, (ln, ld) => (ln + upperN) < (val - absoluteAccuracy) * (ld + upperD));
				}
				else
				{
					// Middle is our best fraction
					if(wholeNum == 0)
					{
						return "(" + middleN.ToString() + "/" + middleD.ToString() + ")";
					}
					return "(" + wholeNum.ToString() + " " + middleN.ToString() + "/" + middleD.ToString() + ")";
				}
			}

		}

		/// Binary seek for the value where f() becomes false.
		/// Used for mixed number calculation. Taken from https://stackoverflow.com/a/32903747
		(int, int) Seek(int a, int b, int aInc, int bInc, Func<int, int, bool> f)
		{
			a += aInc;
			b += bInc;

			if (f(a, b))
			{
				int weight = 1;

				do
				{
					weight *= 2;
					a += aInc * weight;
					b += bInc * weight;
				}
				while (f(a, b));

				do
				{
					weight /= 2;

					int aDec = aInc * weight;
					int bDec = bInc * weight;

					if (!f(a - aDec, b - bDec))
					{
						a -= aDec;
						b -= bDec;
					}
				}
				while (weight > 1);
			}
			return (a,b);
		}

	}
}
