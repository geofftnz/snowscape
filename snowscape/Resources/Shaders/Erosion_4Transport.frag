#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D flowtex;
uniform sampler2D flowdtex;
uniform sampler2D velocitytex;
uniform float texsize;
uniform float evaporationfactor;

in vec2 texcoord;

out vec4 out_Terrain;
out vec4 out_Flow;
out vec4 out_FlowD;


float t = 1.0 / texsize;
float diag = 0.707;

vec4 sampleFlow(vec2 pos)
{
	return texture(flowtex,pos);
}

vec4 sampleFlowD(vec2 pos)
{
	return texture(flowdtex,pos);
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
	vec4 outflowd = sampleFlowD(texcoord);
	vec2 velocity = texture(velocitytex,texcoord).rg;

	//layers.a = sampleSediment(texcoord - normalize(velocity) * t)*0.1;


	// move water/sediment via flows.
	// a proportional amount of sediment flows with the water.

	// subtract outflows
	float totaloutflow = outflow.r + outflow.g + outflow.b + outflow.a +
	                     (outflowd.r + outflowd.g + outflowd.b + outflowd.a) * diag;

	float sedimentoutflow = layers.a * clamp((totaloutflow / layers.b),0.0,1.0);

	layers.a = max(0.0,layers.a - sedimentoutflow);
	layers.b = max(0.0,layers.b - totaloutflow);

	// RGBA = R:top G:right B:bottom A:left
	// RGBA = R:topright G:bottomright B:bottomleft A:topleft

	// add inflow from left block
	vec2 leftpos = texcoord + vec2(-t,0);
	vec4 leftflow = sampleFlow(leftpos);
	vec4 leftcell = sampleLayer(leftpos);
	layers.b += leftflow.g;
	layers.a += leftcell.a * clamp((leftflow.g / leftcell.b),0.0,1.0);

	// add inflow from right block
	vec2 rightpos = texcoord + vec2(t,0);
	vec4 rightflow = sampleFlow(rightpos);
	vec4 rightcell = sampleLayer(rightpos);
	layers.b += rightflow.a;
	layers.a += rightcell.a * clamp((rightflow.a / rightcell.b),0.0,1.0);

	// add inflow from upper block
	vec2 toppos = texcoord + vec2(0,-t);
	vec4 topflow = sampleFlow(toppos);
	vec4 topcell = sampleLayer(toppos);
	layers.b += topflow.b;
	layers.a += topcell.a * clamp((topflow.b / topcell.b),0.0,1.0);

	// add inflow from lower block
	vec2 bottompos = texcoord + vec2(0,t);
	vec4 bottomflow = sampleFlow(bottompos);
	vec4 bottomcell = sampleLayer(bottompos);
	layers.b += bottomflow.r;
	layers.a += bottomcell.a * clamp((bottomflow.r / bottomcell.b),0.0,1.0);

	// add inflow from top right block
	vec2 toprightpos = texcoord + vec2(t,-t);
	float toprightflow = sampleFlowD(toprightpos).b * diag;
	vec4 toprightcell = sampleLayer(toprightpos);
	layers.b += toprightflow;
	layers.a += toprightcell.a * clamp((toprightflow / toprightcell.b),0.0,1.0);

	// add inflow from bottom right block
	vec2 bottomrightpos = texcoord + vec2(t,t);
	float bottomrightflow = sampleFlowD(bottomrightpos).a * diag;
	vec4 bottomrightcell = sampleLayer(bottomrightpos);
	layers.b += bottomrightflow;
	layers.a += bottomrightcell.a * clamp((bottomrightflow / bottomrightcell.b),0.0,1.0);

	// add inflow from bottom left block
	vec2 bottomleftpos = texcoord + vec2(-t,t);
	float bottomleftflow = sampleFlowD(bottomleftpos).r * diag;
	vec4 bottomleftcell = sampleLayer(bottomleftpos);
	layers.b += bottomleftflow;
	layers.a += bottomleftcell.a * clamp((bottomleftflow / bottomleftcell.b),0.0,1.0);

	// add inflow from top left block
	vec2 topleftpos = texcoord + vec2(-t,-t);
	float topleftflow = sampleFlowD(topleftpos).g * diag;
	vec4 topleftcell = sampleLayer(topleftpos);
	layers.b += topleftflow;
	layers.a += topleftcell.a * clamp((topleftflow / topleftcell.b),0.0,1.0);



	// evaporation
	float sedimentprecipitation = max(0.0,layers.a * evaporationfactor);
	layers.a -= sedimentprecipitation;
	layers.g += sedimentprecipitation;
	layers.b *= evaporationfactor;
		
	
	// add some water
	//if (length(texcoord-vec2(0.2,0.75)) < 0.002)
	//{
		//layers.b += 0.05;
	//}
//
	layers.b += 0.0015;



	out_Terrain = layers;
	out_Flow = outflow; // copy flow1 back to flow0 for next iteration.
	out_FlowD = outflowd;
	

}
