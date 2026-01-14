namespace C7GameData {
	using System;
	using System.Diagnostics;
	using System.Collections.Generic;

	public class HeightMap {
		// The actual noise map. Values are between 0 and 255, to make exporting
		// as an image easier when debugging.
		private int[,] noiseMap;

		// When generating the noise we use a fixed size, and then scale that
		// based on the actual map size. This is used for translation.
		private double tilesPerCellX;
		private double tilesPerCellY;

		public int mapWidth;
		public int mapHeight;

		public bool wrapX;
		public bool wrapY;

		public const int NOISE_WIDTH = 128;
		public const int NOISE_HEIGHT = 64;

		public HeightMap(
			int seed,
			int width,
			int height,
			double scale,
			bool wrapX = true,
			bool wrapY = false,
			bool forceLowPointsAtPoles = true
		) {
			// Generate a rectangular map with an arbitrary (power of 2 friendly)
			// size.
			double[,] noiseMapDoubles = generateNoiseMap(seed, NOISE_WIDTH, NOISE_HEIGHT, scale, wrapX, wrapY, forceLowPointsAtPoles);
			noiseMap = normalizeMap(noiseMapDoubles);

			mapWidth = width;
			mapHeight = height;

			this.wrapX = wrapX;
			this.wrapY = wrapY;

			tilesPerCellX = width / (double)NOISE_WIDTH;
			tilesPerCellY = height / (double)NOISE_HEIGHT;
		}

		public int GetHeight(int mapX, int mapY) {
			// Go from map coordinates to our noise map coordinates.
			int x = (int)(mapX / tilesPerCellX);
			int y = (int)(mapY / tilesPerCellY);

			return noiseMap[x, y];
		}

		// Does a binary search to find the height that will give the specified
		// water percentage.
		public int FindSeaLevel(int percentWater) {
			int totalMapTiles = mapWidth * mapHeight;

			int lowThreshold = 0;
			int highThreshold = 255;
			int currentThreshold = 128;

			// Since the scale is 0-255, we need at most 8 iterations.
			for (int i = 0; i < 8; i++) {
				int waterTiles = 0;

				for (int x = 0; x < mapWidth; x++) {
					for (int y = 0; y < mapHeight; y++) {
						if (GetHeight(x, y) < currentThreshold) {
							++waterTiles;
						}
					}
				}

				int currentPercent = (waterTiles * 100) / totalMapTiles;
				if (currentPercent < percentWater) {
					lowThreshold = currentThreshold;
				} else {
					highThreshold = currentThreshold;
				}
				currentThreshold = (lowThreshold + highThreshold) / 2;

				if (lowThreshold >= highThreshold - 1) break;
			}
			return currentThreshold;
		}

		private static int[,] normalizeMap(double[,] noiseMap) {
			double minVal = double.MaxValue;
			double maxVal = double.MinValue;

			for (int x = 0; x < NOISE_WIDTH; x++) {
				for (int y = 0; y < NOISE_HEIGHT; y++) {
					if (noiseMap[x, y] < minVal)
						minVal = noiseMap[x, y];
					if (noiseMap[x, y] > maxVal)
						maxVal = noiseMap[x, y];
				}
			}

			double range = maxVal - minVal;
			int[,] result = new int[NOISE_WIDTH, NOISE_HEIGHT];

			for (int x = 0; x < NOISE_WIDTH; x++) {
				for (int y = 0; y < NOISE_HEIGHT; y++) {
					double normalizedValue = (noiseMap[x, y] - minVal) / range;
					normalizedValue = Math.Max(0.0, Math.Min(1.0, normalizedValue));

					result[x, y] = (int)(normalizedValue * 255);
				}
			}
			return result;
		}

		// See https://ronvalstar.nl/creating-tileable-noise-maps for why we use
		// multiple dimensions when wrapping is enabled.
		private static double[,] generateNoiseMap(int seed, int width, int height, double scale, bool wrapX, bool wrapY, bool forceLowPointsAtPoles) {
			int octaves = 8;
			// The public domain OpenSiplex implementation always
			//   seems to be 0 at 0,0, so let's offset from it.
			double originOffset = 1000;
			double xRadius = (double)width / (System.Math.PI * 2);
			double yRadius = (double)height / (System.Math.PI * 2);
			OpenSimplexNoise noise = new OpenSimplexNoise(seed);
			double[,] noiseField = new double[width, height];

			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					double noiseValue = 0.0;

					// In each octave loop below we adjust the amplitude and
					// frequency to get different levels of detail.
					double amplitude = 1.0;
					double frequency = 1.0;

					for (int i = 0; i < octaves; i++) {
						// Calculate coordinates for this octave. We adjust the
						// scale and offset appropriately.
						double octaveScale = scale * frequency;
						double currentOffset = i * 1000;

						double sampleX=0, sampleY=0, sampleZ=0, sampleW=0;

						if (!(wrapX || wrapY)) {
							sampleX = originOffset + (octaveScale * x) + currentOffset;
							sampleY = originOffset + (octaveScale * y) + currentOffset;
							noiseValue += noise.Evaluate(sampleX, sampleY) * amplitude;
						} else if (wrapX && wrapY) {
							// Recalculate wrapping coords with octaveScale
							double thetaX = ((double)x / (double)width) * (System.Math.PI * 2);
							double cX = originOffset + (octaveScale * xRadius * System.Math.Sin(thetaX));
							double cY = originOffset + (octaveScale * xRadius * System.Math.Cos(thetaX));
							double thetaY = ((double)y / (double)height) * (System.Math.PI * 2);
							double ycX = originOffset + (octaveScale * yRadius * System.Math.Sin(thetaY));
							double ycY = originOffset + (octaveScale * yRadius * System.Math.Cos(thetaY));

							sampleX = cX + currentOffset;
							sampleY = cY + currentOffset;
							sampleZ = ycX + currentOffset; // Use 3rd dim for Y wrap
							sampleW = ycY + currentOffset; // Use 4th dim for Y wrap
							noiseValue += noise.Evaluate(sampleX, sampleY, sampleZ, sampleW) * amplitude;
						} else if (wrapY) {
							// Recalculate wrapping coords with octaveScale
							double thetaY = ((double)y / (double)height) * (System.Math.PI * 2);
							double ycX = originOffset + (octaveScale * yRadius * System.Math.Sin(thetaY));
							double ycY = originOffset + (octaveScale * yRadius * System.Math.Cos(thetaY));
							double oX = originOffset + (octaveScale * x); // Regular X coord

							sampleX = ycX + currentOffset; // Use 1st dim for Y wrap
							sampleY = ycY + currentOffset; // Use 2nd dim for Y wrap
							sampleZ = oX + currentOffset; // Use 3rd dim for regular X
							noiseValue += noise.Evaluate(sampleX, sampleY, sampleZ) * amplitude;
						} else if (wrapX) {
							// Recalculate wrapping coords with octaveScale
							double thetaX = ((double)x / (double)width) * (System.Math.PI * 2);
							double cX = originOffset + (octaveScale * xRadius * System.Math.Sin(thetaX));
							double cY = originOffset + (octaveScale * xRadius * System.Math.Cos(thetaX));
							double oY = originOffset + (octaveScale * y); // Regular Y coord

							sampleX = cX + currentOffset; // Use 1st dim for X wrap
							sampleY = cY + currentOffset; // Use 2nd dim for X wrap
							sampleZ = oY + currentOffset; // Use 3rd dim for regular Y
							noiseValue += noise.Evaluate(sampleX, sampleY, sampleZ) * amplitude;
						}
						noiseField[x, y] = noiseValue;

						// Decrease the amplitude but increase the frequency for
						// each octave.
						amplitude *= .5;
						frequency *= 2;
					}

					// If we're forcing low points at the poles, figure out
					// the distance from the equator, and multiple our noise
					// by 1-(dist^2) to force the values at the poles down.
					if (!wrapY && forceLowPointsAtPoles) {
						double normalizedY = (double)y / (height - 1);
						double distanceFromEquator = Math.Abs(normalizedY - 0.5) * 2.0;
						double polarFactor = 1.0 - Math.Pow(distanceFromEquator, 2.0);
						noiseField[x, y] *= polarFactor;
					}
				}
			}

			return noiseField;
		}
	}
}
