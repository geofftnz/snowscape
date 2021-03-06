﻿#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D flowtex;
uniform sampler2D flowdtex;
uniform sampler2D velocitytex;
uniform float texsize;
uniform float evaporationfactor;
uniform float time;

in vec2 texcoord;

out vec4 out_Terrain;
out vec4 out_Flow;
out vec4 out_FlowD;
out vec4 out_Velocity;


float t = 1.0 / texsize;
float diag = 0.707;


float rand(vec2 co){
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

float rand(vec3 co){
    return fract(sin(dot(co.xyz ,vec3(12.9898,78.233,47.985))) * 43758.5453);
}

// credit: iq/rgba
float hash( float n )
{
    return fract(sin(n)*43758.5453);
}


// credit: iq/rgba
float noise( in vec3 x )
{
    vec3 p = floor(x);
    vec3 f = fract(x);
    f = f*f*(3.0-2.0*f);
    float n = p.x + p.y*57.0 + 113.0*p.z;
    float res = mix(mix(mix( hash(n+  0.0), hash(n+  1.0),f.x),
                        mix( hash(n+ 57.0), hash(n+ 58.0),f.x),f.y),
                    mix(mix( hash(n+113.0), hash(n+114.0),f.x),
                        mix( hash(n+170.0), hash(n+171.0),f.x),f.y),f.z);
    return res;
}



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

vec2 sampleVelocity(vec2 pos)
{
	return texture(velocitytex,pos).xy;
}

void main(void)
{
	// flow RGBA = R:top G:right B:bottom A:left

	// R:hard G:soft B:water A:suspended
	vec4 layers = sampleLayer(texcoord);
	vec4 outflow = sampleFlow(texcoord);
	vec4 outflowd = sampleFlowD(texcoord);
	vec2 velocity = texture(velocitytex,texcoord).xy;

	//layers.a = sampleSediment(texcoord - normalize(velocity) * t)*0.1;


	// move water/sediment via flows.
	// a proportional amount of sediment flows with the water.

	// subtract outflows
	float totaloutflow = outflow.r + outflow.g + outflow.b + outflow.a +
	                     (outflowd.r + outflowd.g + outflowd.b + outflowd.a) * diag;

	float sedimentoutflow = layers.a * clamp((totaloutflow / layers.b),0.0,1.0);

	layers.a = max(0.0,layers.a - sedimentoutflow);
	layers.b = max(0.0,layers.b - totaloutflow);

	vec2 newvelocity = vec2(0.0);

	// RGBA = R:top G:right B:bottom A:left
	// RGBA = R:topright G:bottomright B:bottomleft A:topleft

	// add inflow from left block
	vec2 leftpos = texcoord + vec2(-t,0);
	vec4 leftflow = sampleFlow(leftpos);
	vec4 leftcell = sampleLayer(leftpos);
	float leftamount = clamp((leftflow.g / leftcell.b),0.0,1.0);
	layers.b += leftflow.g;
	layers.a += leftcell.a * leftamount;
	newvelocity += sampleVelocity(leftpos) * leftamount;

	// add inflow from right block
	vec2 rightpos = texcoord + vec2(t,0);
	vec4 rightflow = sampleFlow(rightpos);
	vec4 rightcell = sampleLayer(rightpos);
	float rightamount = clamp((rightflow.a / rightcell.b),0.0,1.0);
	layers.b += rightflow.a;
	layers.a += rightcell.a * rightamount;
	newvelocity += sampleVelocity(rightpos) * rightamount;

	// add inflow from upper block
	vec2 toppos = texcoord + vec2(0,-t);
	vec4 topflow = sampleFlow(toppos);
	vec4 topcell = sampleLayer(toppos);
	float topamount = clamp((topflow.b / topcell.b),0.0,1.0);
	layers.b += topflow.b;
	layers.a += topcell.a * topamount;
	newvelocity += sampleVelocity(toppos) * topamount;

	// add inflow from lower block
	vec2 bottompos = texcoord + vec2(0,t);
	vec4 bottomflow = sampleFlow(bottompos);
	vec4 bottomcell = sampleLayer(bottompos);
	float bottomamount = clamp((bottomflow.r / bottomcell.b),0.0,1.0);
	layers.b += bottomflow.r;
	layers.a += bottomcell.a * bottomamount;
	newvelocity += sampleVelocity(bottompos) * bottomamount;

	// add inflow from top right block
	vec2 toprightpos = texcoord + vec2(t,-t);
	float toprightflow = sampleFlowD(toprightpos).b * diag;
	vec4 toprightcell = sampleLayer(toprightpos);
	float toprightamount = clamp((toprightflow / toprightcell.b),0.0,1.0);
	layers.b += toprightflow;
	layers.a += toprightcell.a * toprightamount;
	newvelocity += sampleVelocity(toprightpos) * toprightamount;

	// add inflow from bottom right block
	vec2 bottomrightpos = texcoord + vec2(t,t);
	float bottomrightflow = sampleFlowD(bottomrightpos).a * diag;
	vec4 bottomrightcell = sampleLayer(bottomrightpos);
	float bottomrightamount = clamp((bottomrightflow / bottomrightcell.b),0.0,1.0);
	layers.b += bottomrightflow;
	layers.a += bottomrightcell.a * bottomrightamount;
	newvelocity += sampleVelocity(bottomrightpos) * bottomrightamount;

	// add inflow from bottom left block
	vec2 bottomleftpos = texcoord + vec2(-t,t);
	float bottomleftflow = sampleFlowD(bottomleftpos).r * diag;
	vec4 bottomleftcell = sampleLayer(bottomleftpos);
	float bottomleftamount = clamp((bottomleftflow / bottomleftcell.b),0.0,1.0);
	layers.b += bottomleftflow;
	layers.a += bottomleftcell.a * bottomleftamount;
	newvelocity += sampleVelocity(bottomleftpos) * bottomleftamount;

	// add inflow from top left block
	vec2 topleftpos = texcoord + vec2(-t,-t);
	float topleftflow = sampleFlowD(topleftpos).g * diag;
	vec4 topleftcell = sampleLayer(topleftpos);
	float topleftamount = clamp((topleftflow / topleftcell.b),0.0,1.0);
	layers.b += topleftflow;
	layers.a += topleftcell.a * topleftamount;
	newvelocity += sampleVelocity(topleftpos) * topleftamount;



	// evaporation
	
	//float sedimentprecipitation = max(0.0,layers.a * (evaporationfactor / (1.0 + length(velocity.xy))));
	//layers.a -= sedimentprecipitation;
	//layers.g += sedimentprecipitation;
	//layers.b *= evaporationfactor;
		//
	
	// add some water
	//if (length(texcoord-vec2(0.2,0.75)) < 0.002)
	//{
		//layers.b += 0.05;
	//}
//
	//layers.b += 0.0015;

	layers.b += 0.002 * max(0.0,noise(vec3(texcoord.xy * 177.0,time * 2783.0)) - 0.4);



	out_Terrain = layers;
	out_Flow = outflow; // copy flow1 back to flow0 for next iteration.
	out_FlowD = outflowd;
	out_Velocity = vec4(velocity,0.0,0.0);
	

}
