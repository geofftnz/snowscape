#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D sliptex;
uniform float texsize;

in vec2 texcoord;

out vec4 out_Terrain;

float t = 1.0 / texsize;

vec4 sampleFlow(vec2 pos)
{
	return texture(sliptex,pos);
}

vec4 sampleLayer(vec2 pos)
{
	return texture(terraintex,pos);
}

void main(void)
{
	// flow RGBA = R:top G:right B:bottom A:left

	// R:hard G:soft B:water A:suspended
	vec4 layers = sampleLayer(texcoord);
	vec4 outflow = sampleFlow(texcoord);

	// move loose material via flows.
	// a proportional amount of sediment flows with the water.

	// subtract outflows
	float totaloutflow = outflow.r + outflow.g + outflow.b + outflow.a;
	layers.b = max(0.0,layers.b - totaloutflow);

	// add inflow from left block
	vec2 leftpos = texcoord + vec2(-t,0);
	vec4 leftflow = sampleFlow(leftpos);
	vec4 leftcell = sampleLayer(leftpos);
	layers.b += leftflow.g;

	// add inflow from right block
	vec2 rightpos = texcoord + vec2(t,0);
	vec4 rightflow = sampleFlow(rightpos);
	vec4 rightcell = sampleLayer(rightpos);
	layers.b += rightflow.a;

	// add inflow from upper block
	vec2 toppos = texcoord + vec2(0,-t);
	vec4 topflow = sampleFlow(toppos);
	vec4 topcell = sampleLayer(toppos);
	layers.b += topflow.b;

	// add inflow from lower block
	vec2 bottompos = texcoord + vec2(0,t);
	vec4 bottomflow = sampleFlow(bottompos);
	vec4 bottomcell = sampleLayer(bottompos);
	layers.b += bottomflow.r;

	out_Terrain = layers;

}
