#version 140
precision highp float;

uniform sampler2D terrainTex;
in vec2 texcoord;

out float out_Height;

void main(void)
{
	vec4 t = texture2D(terrainTex,texcoord);
	out_Height = t.r + t.g + t.b;
}
