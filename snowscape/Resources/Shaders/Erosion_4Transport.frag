#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D flowtex;
uniform sampler2D velocitytex;
uniform float texsize;

in vec2 texcoord;

out vec4 out_Terrain;
out vec4 out_Flow;



float t = 1.0 / texsize;

float sampleSediment(vec2 pos)
{

	pos *= texsize;

	vec2 ipos = floor(pos);
	vec2 fpos = pos - floor(pos);

	ipos *= t;

	float s00 = texture(terraintex,ipos).a;
	float s10 = texture(terraintex,ipos + vec2(t,0)).a;
	float s11 = texture(terraintex,ipos + vec2(t,t)).a;
	float s01 = texture(terraintex,ipos + vec2(0,t)).a;

	return mix(mix(s00,s10,fpos.x),mix(s01,s11,fpos.x),fpos.y);
}

vec4 sampleFlow(vec2 pos)
{
	return texture(flowtex,pos);
}

vec4 sampleLayer(vec2 pos)
{
	return texture(terraintex,pos);
}

void main(void)
{
	// flow RGBA = R:top G:right B:bottom A:left

	// R:hard G:soft B:water A:suspended
	vec4 layers = texture(terraintex,texcoord);
	vec4 outflow = sampleFlow(texcoord);
	vec2 velocity = texture(velocitytex,texcoord).rg;

	//layers.a = sampleSediment(texcoord - normalize(velocity) * t)*0.1;


	// move water/sediment via flows.
	// a proportional amount of sediment flows with the water.

	// subtract outflows
	float totaloutflow = outflow.r + outflow.g + outflow.b + outflow.a;
	if (totaloutflow>0) // we lose all sediment unless we have no outflows.
	{
		layers.a = 0.0;
	}
	layers.b -= min(layers.b,totaloutflow);

	// add inflow from left block
	vec2 leftpos = texcoord + vec2(-t,0);
	vec4 leftflow = sampleFlow(leftpos);
	vec4 leftcell = sampleLayer(leftpos);
	layers.b += leftflow.g;
	layers.a += leftcell.a * (leftflow.g / (leftflow.r + leftflow.g + leftflow.b + leftflow.a));

	// add inflow from right block
	vec2 rightpos = texcoord + vec2(t,0);
	vec4 rightflow = sampleFlow(rightpos);
	vec4 rightcell = sampleLayer(rightpos);
	layers.b += rightflow.a;
	layers.a += rightcell.a * (rightflow.a / (rightflow.r + rightflow.g + rightflow.b + rightflow.a));

	// add inflow from upper block
	vec2 toppos = texcoord + vec2(0,-t);
	vec4 topflow = sampleFlow(toppos);
	vec4 topcell = sampleLayer(toppos);
	layers.b += topflow.b;
	layers.a += topcell.a * (topflow.b / (topflow.r + topflow.g + topflow.b + topflow.a));

	// add inflow from lower block
	vec2 bottompos = texcoord + vec2(0,t);
	vec4 bottomflow = sampleFlow(bottompos);
	vec4 bottomcell = sampleLayer(bottompos);
	layers.b += bottomflow.r;
	layers.a += bottomcell.a * (bottomflow.r / (bottomflow.r + bottomflow.g + bottomflow.b + bottomflow.a));


/*
	// add outflows from surrounding blocks.
	// left cell, take right outflow
	layers.b += sampleFlow(texcoord + vec2(-t,0)).g;
	// right cell, take left outflow
	layers.b += sampleFlow(texcoord + vec2(t,0)).a;

	// top cell, take bottom outflow
	layers.b += sampleFlow(texcoord + vec2(0,-t)).b;

	// bottom cell, take top outflow
	layers.b += sampleFlow(texcoord + vec2(0,t)).r;
*/



	out_Terrain = layers;
	out_Flow = outflow; // copy flow1 back to flow0 for next iteration.
	

}
