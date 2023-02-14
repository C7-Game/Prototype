shader_type canvas_item;

uniform sampler2D palette;
uniform sampler2D civColorWhitePalette;
uniform sampler2D indices;
uniform vec2 relSpriteSize; // sprite size relative to the entire sheet
uniform vec2 spriteXY; // coordinates of the sprite to be drawn, in number of sprites not pixels
uniform vec3 civColor;


vec4 sampleCivTintedColor(vec2 paletteCoords)
{
	return vec4(civColor, 1.0) * texture(civColorWhitePalette, paletteCoords);
}

void vertex()
{
	//UV = (spriteXY + UV) * relSpriteSize;
}

void fragment()
{
	int colorIndex = floatBitsToInt(texture(indices, UV).r);
	if (colorIndex >= 253) // indices 254 and 255 are transparent
		discard;
	else if (colorIndex >= 240) // indices in [240, 253] are shadows
		COLOR = vec4(0.0, 0.0, 0.0, float(16 * (255 - colorIndex)) / 255.0);
	else if (colorIndex >= 224) // indices in [224, 239] are smoke
		COLOR = vec4(0.0, 0.0, 0.0, 0.1 + 0.032 * float(colorIndex - 224));
	else {
		vec2 paletteCoords = vec2(float(colorIndex % 16), float(colorIndex / 16)) / 16.0;
		bool tintedByCiv = (colorIndex < 16) || ((colorIndex < 64) && (colorIndex % 2 == 0));
		tintedByCiv = false;
		if (tintedByCiv) {
			COLOR = sampleCivTintedColor(paletteCoords);
		} else {
			COLOR = texture(palette, paletteCoords);
		}
	}
}
