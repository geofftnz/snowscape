﻿/*
	Slips the top layer of a multi-layer terrain

	R = hard material height
	G = soft material amount

	texsize = dimension of textures
	threshold = min amount of soft material required to have the required weight to slip
	maxdiff = maximum slope differential - material over this amount will slip
	sliprate = what proportion of material to shift
	maxslip = maximum amount of material to shift
	minslip = minimum amount of material to shift

	requires 2 buffer textures, one for ortho-outflows, other for diagonal outflows

	Ortho: RGBA = N S W E
	Diag: RGBA = NW NE SW SE
*/

//|Outflow

#version 140
precision highp float;

uniform sampler2D terraintex;

uniform float texsize;
//uniform float threshold;
uniform float maxdiff;
uniform float sliprate;
uniform float minslip;
uniform float maxslip;
uniform float saturationslip;
uniform float saturationthreshold;
uniform float saturationrate;

in vec2 texcoord;

out vec4 out_SlipO;
out vec4 out_SlipD;

float t = 1.0 / texsize;
vec3 tt = vec3(-t,0,t);
float diag = 0.707;

float sampleHeight(vec2 pos)
{
	vec4 l = texture(terraintex,pos);
	return l.r + l.g;
}

float getHeight(vec4 l)
{
	return l.r + l.g;
}

void main(void)
{
	vec4 l = texture(terraintex,texcoord);

	vec4 ter_n = texture(terraintex,texcoord + tt.yx);
	vec4 ter_s = texture(terraintex,texcoord + tt.yz);
	vec4 ter_w = texture(terraintex,texcoord + tt.xy);
	vec4 ter_e = texture(terraintex,texcoord + tt.zy);
	vec4 ter_nw = texture(terraintex,texcoord + tt.xx);
	vec4 ter_ne = texture(terraintex,texcoord + tt.zx);
	vec4 ter_sw = texture(terraintex,texcoord + tt.xz);
	vec4 ter_se = texture(terraintex,texcoord + tt.zz);

	float saturation = l.b;
	saturation += ter_n.b;
	saturation += ter_s.b;
	saturation += ter_w.b;
	saturation += ter_e.b;
	saturation += (ter_nw.b + ter_ne.b + ter_sw.b + ter_se.b) * diag;
	saturation = max(0.0,saturation - saturationthreshold);
	
	float h = l.r + l.g - maxdiff / (1.0 + saturation * saturationslip);    // reduce height by our max slope - makes following calculation easier


	vec4 outflowO = vec4(0);
	vec4 outflowD = vec4(0);
	
	outflowO.r = max(0,h - getHeight(ter_n));	 // N
	outflowO.g = max(0,h - getHeight(ter_s));	 // S
	outflowO.b = max(0,h - getHeight(ter_w));	 // W
	outflowO.a = max(0,h - getHeight(ter_e));	 // E

	outflowD.r = max(0,h - getHeight(ter_nw));	 // NW
	outflowD.g = max(0,h - getHeight(ter_ne));	 // NE
	outflowD.b = max(0,h - getHeight(ter_sw));	 // SW
	outflowD.a = max(0,h - getHeight(ter_se));	 // SE
	outflowD *= diag; // correction for diagonal transfers

	float ptotal = dot(outflowO,vec4(1)) + dot(outflowD,vec4(1)); // add components
	
	float pscale = (min(ptotal,l.g) * clamp(1.0 / ptotal,0.0,1.0)) * min(0.02, sliprate + saturation * saturationrate);

	out_SlipO = outflowO * pscale;
	out_SlipD = outflowD * pscale;
}


//|Transport

#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D flowOtex;
uniform sampler2D flowDtex;

uniform float texsize;

in vec2 texcoord;

out vec4 out_Terrain;

float t = 1.0 / texsize;

//	Ortho: RGBA = N S W E
//	Diag: RGBA = NW NE SW SE

// N S W E = yx yz xy zy
// NW NE SW SE = xx zx xz zz

void main(void)
{
	vec4 terrain = texture(terraintex,texcoord);
	
	vec3 tt = vec3(-t,0,t); // offset swizzle

	// subtract our outflow
	terrain.g = max(0.0,
					terrain.g - 
					(
						dot(texture(flowOtex,texcoord),vec4(1.0)) + 
						dot(texture(flowDtex,texcoord),vec4(1.0))
					) 
				);

	// inflow from N (S)
	terrain.g += texture(flowOtex,texcoord + tt.yx).g;

	// inflow from S (N)
	terrain.g += texture(flowOtex,texcoord + tt.yz).r;

	// inflow from W (E)
	terrain.g += texture(flowOtex,texcoord + tt.xy).a;

	// inflow from E (W)
	terrain.g += texture(flowOtex,texcoord + tt.zy).b;

	// inflow from NW (SE)
	terrain.g += texture(flowDtex,texcoord + tt.xx).a;

	// inflow from NE (SW)
	terrain.g += texture(flowDtex,texcoord + tt.zx).b;

	// inflow from SW (NE)
	terrain.g += texture(flowDtex,texcoord + tt.xz).g;

	// inflow from SE (NW)
	terrain.g += texture(flowDtex,texcoord + tt.zz).r;

	out_Terrain = terrain;
}



//|SnowOutflow

#version 140
precision highp float;

uniform sampler2D terraintex;

uniform float texsize;
uniform float threshold;
uniform float maxdiff;
uniform float sliprate;
uniform float minslip;
uniform float maxslip;

in vec2 texcoord;

out vec4 out_SlipO;
out vec4 out_SlipD;

float t = 1.0 / texsize;
float diag = 0.707;

float sampleHeight(vec2 pos)
{
	vec4 l = texture(terraintex,pos);
	return l.r + l.g + l.b + l.a;
}

void main(void)
{
	vec4 l = texture(terraintex,texcoord);
	float h = l.r + l.g + l.b + l.a - maxdiff;    // reduce height by our max slope - makes following calculation easier

	vec3 tt = vec3(-t,0,t);

	vec4 outflowO = vec4(0);
	vec4 outflowD = vec4(0);
	
	outflowO.r = max(0,h - sampleHeight(texcoord + tt.yx));	 // N
	outflowO.g = max(0,h - sampleHeight(texcoord + tt.yz));	 // S
	outflowO.b = max(0,h - sampleHeight(texcoord + tt.xy));	 // W
	outflowO.a = max(0,h - sampleHeight(texcoord + tt.zy));	 // E

	outflowD.r = max(0,h - sampleHeight(texcoord + tt.xx));	 // NW
	outflowD.g = max(0,h - sampleHeight(texcoord + tt.zx));	 // NE
	outflowD.b = max(0,h - sampleHeight(texcoord + tt.xz));	 // SW
	outflowD.a = max(0,h - sampleHeight(texcoord + tt.zz));	 // SE
	outflowD *= diag; // correction for diagonal transfers

	float ptotal = dot(outflowO,vec4(1)) + dot(outflowD,vec4(1)); // add components
	
	float pscale = (min(ptotal,l.a) * clamp(1.0 / ptotal,0.0,1.0)) * sliprate;

	out_SlipO = outflowO * pscale;
	out_SlipD = outflowD * pscale;
}


//|SnowTransport

#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D flowOtex;
uniform sampler2D flowDtex;

uniform float texsize;

in vec2 texcoord;

out vec4 out_Terrain;

float t = 1.0 / texsize;

//	Ortho: RGBA = N S W E
//	Diag: RGBA = NW NE SW SE

// N S W E = yx yz xy zy
// NW NE SW SE = xx zx xz zz

void main(void)
{
	vec4 terrain = texture(terraintex,texcoord);
	
	vec3 tt = vec3(-t,0,t); // offset swizzle

	// subtract our outflow
	terrain.a = max(0.0,
					terrain.a - 
					(
						dot(texture(flowOtex,texcoord),vec4(1.0)) + 
						dot(texture(flowDtex,texcoord),vec4(1.0))
					) 
				);

	// inflow from N (S)
	terrain.a += texture(flowOtex,texcoord + tt.yx).g;

	// inflow from S (N)
	terrain.a += texture(flowOtex,texcoord + tt.yz).r;

	// inflow from W (E)
	terrain.a += texture(flowOtex,texcoord + tt.xy).a;

	// inflow from E (W)
	terrain.a += texture(flowOtex,texcoord + tt.zy).b;

	// inflow from NW (SE)
	terrain.a += texture(flowDtex,texcoord + tt.xx).a;

	// inflow from NE (SW)
	terrain.a += texture(flowDtex,texcoord + tt.zx).b;

	// inflow from SW (NE)
	terrain.a += texture(flowDtex,texcoord + tt.xz).g;

	// inflow from SE (NW)
	terrain.a += texture(flowDtex,texcoord + tt.zz).r;

	out_Terrain = terrain;
}






