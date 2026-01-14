using System;
using System.Collections.Generic;
using C7GameData;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace EngineTests.GameData;

public class HeightMapTest {
	public static void SaveNoiseMapAsPng(List<HeightMap> heightMaps, string filePath) {
		using (Image<Bgra32> image = new(heightMaps[0].mapWidth, heightMaps[0].mapHeight * heightMaps.Count)) {
			for (int i = 0; i < heightMaps.Count; ++i) {
				for (int x = 0; x < heightMaps[i].mapWidth; x++) {
					for (int y = 0; y < heightMaps[i].mapHeight; y++) {
						byte grayScale = (byte)(heightMaps[i].GetHeight(x, y));
						image[x, y + i * heightMaps[0].mapHeight] = new Bgra32(grayScale, grayScale, grayScale, (byte)255);
					}
				}
			}

			image.SaveAsPng(filePath);
		}

		Console.WriteLine($"Noise map saved to: {filePath}");
	}

	[Fact]
	public void HeightMapGeneration() {
		List<HeightMap> hms = new();

		// Roughly archipelago shaped.
		for (int i = 0; i < 4; ++i) {
			hms.Add(new HeightMap(seed: new Random().Next(int.MaxValue), width: 100, height: 100, scale: 0.2));
		}

		// Roughly continents shaped.
		for (int i = 0; i < 4; ++i) {
			hms.Add(new HeightMap(seed: new Random().Next(int.MaxValue), width: 100, height: 100, scale: 0.05));
		}

		// Roughly pangaea shaped.
		for (int i = 0; i < 4; ++i) {
			hms.Add(new HeightMap(seed: new Random().Next(int.MaxValue), width: 100, height: 100, scale: 0.02));
		}

		// Save the noise maps as a png to make it easier to understand how the
		// scale parameter (and other height map parameters) change things.
		SaveNoiseMapAsPng(hms, "debug_noise_map.png");
	}
}
