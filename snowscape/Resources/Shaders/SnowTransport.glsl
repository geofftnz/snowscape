/*
	GPU Snow transport model
	(c)2014 Geoff Thornburrow

	Parameters:
	- Wind vector
	- Snowfall rate
	- Temperature (? maybe later for melt/freeze)

	Terrain:
	R: hard
	G: soft
	B: packed snow
	A: powder



	Steps:

	1: Snowfall
	2: Powder slip
	3: Powder compaction

	
*/

//|SnowFall
#version 140
precision highp float;

uniform sampler2D terraintex;
uniform float snowfallrate;
in vec2 texcoord;
out vec4 out_Terrain;


void main(void)
{
	vec4 layers = texture(terraintex,texcoord);

	layers.a += snowfallrate;

	out_Terrain = layers;
}
