using Godot;
using System;
using C7GameData;

// A utility class for rendering pop heads.
public class PopHead {
	public record struct TextureKey {
		public CityResident cityResident;
		public int eraNum;
	}

	public const int HEAD_SIZE = 48;

	public static ImageTexture GetTexture(CityResident cityResident, int eraNum) {
		return TextureLoader.Load("popheads", new TextureKey() { cityResident = cityResident, eraNum = eraNum });
	}
}
