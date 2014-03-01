#version 140
precision highp float;

uniform sampler2D heightTexture;
uniform int baseLevel;
uniform float baseLevelWidth;
in vec2 texcoord;

out float out_MaxHeight;

float texel = 1.0 / baseLevelWidth;

void main(void)
{
	float t00 = texture2D(heightTexture,vec2(texcoord.x,texcoord.y)).r;
	float t01 = texture2D(heightTexture,vec2(texcoord.x,texcoord.y + texel)).r;
	float t10 = texture2D(heightTexture,vec2(texcoord.x + texel,texcoord.y)).r;
	float t11 = texture2D(heightTexture,vec2(texcoord.x + texel,texcoord.y + texel)).r;

	out_MaxHeight = max(max(t00,t01),max(t10,t11));
}
