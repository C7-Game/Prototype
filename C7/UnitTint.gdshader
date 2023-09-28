shader_type canvas_item;

render_mode unshaded;

uniform vec3 tintColor;

vec4 applyTint(vec4 color)
{
	return vec4(tintColor, 1.0) * color;
}

void fragment() {
	COLOR = applyTint(COLOR);
}
