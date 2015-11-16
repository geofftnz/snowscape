/*
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

//|WaterOutflow
/*
	Attempts to equalize water height.

	- determine lowest neighbour
	- determine amount of material to shift to equalize heights (half of difference)
	- move that amount of water
*/
#version 140
precision highp float;

uniform sampler2D terraintex;

uniform float texsize;
uniform float sliprate;

in vec2 texcoord;

out vec4 out_SlipO;
out vec4 out_SlipD;

float t = 1.0 / texsize;
vec3 tt = vec3(-t,0,t);
float diag = 0.707;

float sampleHeight(vec2 pos)
{
	vec4 l = texture(terraintex,pos);
	return l.r + l.g + l.b;
}

float getHeight(vec4 l)
{
	return l.r + l.g + l.b;
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

	float h = getHeight(l);

	float diff_n  = max(0.0,h - getHeight(ter_n ));
	float diff_s  = max(0.0,h - getHeight(ter_s ));
	float diff_w  = max(0.0,h - getHeight(ter_w ));
	float diff_e  = max(0.0,h - getHeight(ter_e ));
	float diff_nw = max(0.0,h - getHeight(ter_nw)) * diag;
	float diff_ne = max(0.0,h - getHeight(ter_ne)) * diag;
	float diff_sw = max(0.0,h - getHeight(ter_sw)) * diag;
	float diff_se = max(0.0,h - getHeight(ter_se)) * diag;

	float max_fall = max(max(max(diff_n,diff_s),max(diff_w,diff_e)),max(max(diff_nw,diff_ne),max(diff_sw,diff_se)));

	max_fall *= 0.95;  //need to exchange half of difference to equalize

	vec4 outflowO = vec4(0);
	vec4 outflowD = vec4(0);

	if (max_fall > 0.0)
	{	
		float water_shift = min(max_fall * 0.5,l.b);  // can only shift as much water as we have
		//max_fall *= 0.99;

		outflowO.r = step(max_fall,diff_n) * water_shift; water_shift = max(0.0,water_shift - outflowO.r); 	 // N
		outflowO.g = step(max_fall,diff_s) * water_shift; water_shift = max(0.0,water_shift - outflowO.g);	 // S
		outflowO.b = step(max_fall,diff_w) * water_shift; water_shift = max(0.0,water_shift - outflowO.b);	 // W
		outflowO.a = step(max_fall,diff_e) * water_shift; water_shift = max(0.0,water_shift - outflowO.a);	 // E
														  
		outflowD.r = step(max_fall,diff_nw) * water_shift; water_shift = max(0.0,water_shift - outflowD.r);	 // NW
		outflowD.g = step(max_fall,diff_ne) * water_shift; water_shift = max(0.0,water_shift - outflowD.g);	 // NE
		outflowD.b = step(max_fall,diff_sw) * water_shift; water_shift = max(0.0,water_shift - outflowD.b);	 // SW
		outflowD.a = step(max_fall,diff_se) * water_shift; 	 // SE
	}

	out_SlipO = outflowO;
	out_SlipD = outflowD;
}

//|WaterTransport

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
	terrain.b = max(0.0,
					terrain.b - 
					(
						dot(texture(flowOtex,texcoord),vec4(1.0)) + 
						dot(texture(flowDtex,texcoord),vec4(1.0))
					) 
				);

	// inflow from N (S)
	terrain.b += texture(flowOtex,texcoord + tt.yx).g;

	// inflow from S (N)
	terrain.b += texture(flowOtex,texcoord + tt.yz).r;

	// inflow from W (E)
	terrain.b += texture(flowOtex,texcoord + tt.xy).a;

	// inflow from E (W)
	terrain.b += texture(flowOtex,texcoord + tt.zy).b;

	// inflow from NW (SE)
	terrain.b += texture(flowDtex,texcoord + tt.xx).a;

	// inflow from NE (SW)
	terrain.b += texture(flowDtex,texcoord + tt.zx).b;

	// inflow from SW (NE)
	terrain.b += texture(flowDtex,texcoord + tt.xz).g;

	// inflow from SE (NW)
	terrain.b += texture(flowDtex,texcoord + tt.zz).r;

	out_Terrain = terrain;
}





