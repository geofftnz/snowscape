﻿// colour: rgb, material
// normal+?
// shading: [roughness|reflection], specexp, specpwr, sparkle
// lighting - shadow, AO, emmissive, subsurface

//|Common

float t = 1.0 / boxparam.x;
const vec2 detailScale = vec2(0.2,0.2);  // detailTexScale is uniform
const float sampleOfs = 1.0/1024.0;
const float minduv = 0.0001;


float SmoothShadow(float heightdiff)
{
	return smoothstep(-0.5,-0.02,heightdiff);
}

float getHighDetailBlendFactor(vec4 pos)
{
	return 1.0 - smoothstep(80.0,800.0,pos.z);
}

vec2 getDetailTexcoord(vec2 p)
{
	return p * 256.0;
}

const float detailSampleOffset = 4.0 / 1024.0;

#include "noise2d.glsl"

float sfbm(vec2 pos)
{
	float a = snoise(pos); 
	a += snoise(pos * 2.0) * 0.5; 
	a += snoise(pos * 4.0) * 0.25; 
	a += snoise(pos * 8.0) * 0.125; 
	//a += snoise(pos * 16.0) * 0.0625; 
	//a += snoise(pos * 32.0) * 0.03125; 
	return a;
}


float bedrockHeight = 0.5;
// gets a tuple of material (x) and displacement (y) for a given point.
//
// todo: implement as material stack
//
// pos: base terrain pos (bicubic interpolation of base terrain) - used as noise source coordinate
// basenormal: base terrain normal (bicubic interpolation of base terrain) - used for general slope
// detailnormal: transformed normal of
// param: thickness of terrain layers
// scale: resolution (x) and height (y) of detail
// dt: 4-component noise texture sample
vec2 getDetailHeightSample(vec3 pos, vec3 basenormal, vec3 detailnormal, vec4 param, vec2 scale, vec4 dt)
{

	float dirtdepth = param.r * 16.0;
	float waterdepth = 0.0;

	float wateramount = param.g + param.b*0.02;
	if (wateramount > 0.03) return vec2(0.2,-dt.b*0.005 + wateramount * 2.0);
	

	//float upperLayers = dirtdepth + waterdepth;// * (0.5 + 0.5 * clamp(detailnormal.y,0.0,1.0));

	//upperLayers = max(0.0,upperLayers - (0.02*(1.0 - detailnormal.y)));

	//float baselevel = -upperLayers;  // reduce height by total loose material amount
	vec2 rock = vec2(0.0, -(dirtdepth + waterdepth) + dt.r * bedrockHeight + (max(0.0,dt.g - 0.7)) * 0.5);
	//vec2 dirt = vec2(0.1, (dt.g) * 0.2);
	vec2 dirt = vec2(0.1, -waterdepth + (dt.g * dt.g) * 0.5 + detailnormal.y * 0.05 );
	//vec2 dirt = vec2(0.1, - basenormal.y * 0.5);
	vec2 water = vec2(0.2,0.0);

	// find highest sample
	vec2 md = rock;
	md = dirt.y > md.y ? dirt : md;
	//md = water.y > md.y ? water : md;
	//md = detailnormal.y > 0.9 ? dirt:md;

	return md;
}

vec2 getDetailHeightSample(vec3 pos, vec3 basenormal, vec3 detailnormal, vec4 param, vec2 scale)
{
	// get noise texture for this location
	return getDetailHeightSample(pos,basenormal,detailnormal,param,scale,textureLod(detailTex,pos.xz * 0.125,0));
}


// gets the normal of the underlying rock layer, untransformed by the patch normal
// 
vec3 getDetailBedrockNormal(vec2 pos, float t_)
{
	vec3 t = vec3(-t_,0.0,t_);
	pos *= 0.125;  // must match above
	 
	float h1 = textureLod(detailTex,pos + t.xy,0).r * bedrockHeight;  // must match coefficients above
	float h2 = textureLod(detailTex,pos + t.zy,0).r * bedrockHeight;  // must match coefficients above
	float h3 = textureLod(detailTex,pos + t.yx,0).r * bedrockHeight;  // must match coefficients above
	float h4 = textureLod(detailTex,pos + t.yz,0).r * bedrockHeight;  // must match coefficients above
	
	return normalize(vec3(h2-h1,2.0*0.125,h4-h3));  // must match below
}

vec2 getDetailHeightSample(vec2 pos, vec3 basenormal, vec3 detailnormal, vec4 param, vec2 scale)
{
	return getDetailHeightSample(vec3(pos.x,0.0,pos.y), basenormal, detailnormal, param, scale);
}

struct DetailSample
{
	vec3 normal;
	vec2 materialdisp;
	vec4 diffuse;  // RGB = diffuse, A = material ID
	vec4 shading; //R:[roughness|reflection], G:specexp, B:specpwr, A:sparkle
};


vec3 getGrassColour(float _wind, float water, float altitude, float soildepth, float slope, vec4 noiseSample)
{
	vec3 grey_rock = vec3(0.541798356,0.5604991522,0.4534564755);
	vec3 dry_grass = vec3(0.7592995507,0.6940805198,0.3066347662);
	vec3 dry_grass2 = vec3(0.4704402453,0.5731588751,0.1604435107);
	vec3 dry_grass3 = vec3(0.1801442892,0.2888159728,0.05112205006);
	vec3 dark_grass = vec3(0.04298701016,0.091518353,0.01039780229);

	float wind = 1.0 - clamp(((1.0-_wind)*4.0),0.0,1.0);
	float temperature = 1.0 / (1.0 + altitude*0.01);

	float steepness = 1.0 - slope; //acos(slope) / 3.1415927;
	float scrub = (2.0 / (1.0 + steepness*6.0)) * clamp((soildepth*32.0),0.1,1.0) * clamp(temperature,0.0,1.0);


	// calculate available moisture from water and wind estimate (wind based on AO)
	float moisture = sqrt(water) * (1.2 - wind) * (0.6+clamp(soildepth * 128.0,0.05,0.4)) * (0.3+temperature);

	vec3 grasscol = mix(grey_rock,dry_grass,clamp(moisture * 4.0 , 0.0, 1.0));
	grasscol = mix(grasscol,dry_grass2,clamp((moisture-0.25) * 4.0 , 0.0, 1.0));
	grasscol = mix(grasscol,dry_grass3,clamp((moisture-0.5) * 2.0 , 0.0, 1.0));
	//grasscol = mix(grasscol,dark_grass,clamp((moisture-0.75) * 4.0 , 0.0, 1.0));
	//vec3 grasscol = vec3(moisture,0.0,0.0);
	//vec3 grasscol = mix(vec3(1.0,0.0,0.0),vec3(0.0,1.0,0.0),clamp(moisture,0.0,1.0));
	
	grasscol = mix(vec3(1.0,0.0,0.0),vec3(0.0,1.0,0.0),clamp(scrub,0.0,1.0));

	return grasscol;
}


void getMaterial(float material, vec3 pos, vec3 basenormal, vec3 detailnormal, vec4 param, float AO, vec4 noiseSample,  out vec4 diffuse, out vec4 shading)
{
	vec4 hfnoise = snoise(pos.xz * 256.0);

	

	if (material < 0.01) // rock
	{
		vec3 colrock = vec3(0.1,0.08,0.06);
		vec3 colgrass = getGrassColour(AO, param.b * noiseSample.b, pos.y, param.r * noiseSample.b, basenormal.y, noiseSample);
		//vec3 colgrass = vec3(0.3,0.28,0.1);
		float grassthreshold = max(0.6,0.9 - param.r*4.0) - noiseSample.b * 0.4 - hfnoise * 0.05;
		float grassmix = smoothstep(grassthreshold,grassthreshold+0.05,detailnormal.y);
		
		diffuse = vec4(mix(colrock,colgrass,grassmix),material);
		shading = vec4(0.9,1.0,0.0,0.0);

		return;
	}
	if (material < 0.11) // dirt
	{
		vec3 colgrass = getGrassColour(AO, param.b, pos.y, param.r, basenormal.y, noiseSample);
		//float soildepth = clamp(param.r * 16.0,0.0,1.0);
		//float moisture = clamp(param.b*1.2,0.0,1.0);
		//return mix(vec3(0.3,0.28,0.1),vec3(0.1,0.15,0.02), soildepth);
		//mix(vec3(0.3,0.28,0.1),vec3(0.1,0.15,0.02), moisture)
		diffuse = vec4(colgrass,material);
		shading = vec4(0.8,2.0,0.1,0.0);
		
		return;
	}
	if (material < 0.21) // water
	{
		//return vec3(0.5,0.5,1.0);
		diffuse = vec4(vec3(0.5,0.5,1.0)*0.0,material);
		shading = vec4(0.1,8.0,0.3,0.0);
		return;
	}

	diffuse = vec4(vec3(1.0),material);
	shading = vec4(0.0);

	return ; // default
}


DetailSample sampleDetail(vec3 pos, vec3 basenormal, vec3 detailnormal, vec4 param, float AO, vec2 scale, float t_, mat3 surfaceBasis)
{
	DetailSample res;
	vec3 t = vec3(-t_,0.0,t_);

	vec4 noiseSample = textureLod(detailTex,pos.xz * 0.125,0);

	vec2 h0 = getDetailHeightSample(pos,basenormal,detailnormal,param,scale,noiseSample);

	//   3
	// 1 0 2
	//   4
	// get adjacent samples
	// todo: approximate by using the same basenormal & param. Ideally these should be fetched from the texture again.
	float h1 = getDetailHeightSample(pos + t.xyy,basenormal,detailnormal,param,scale).y;
	float h2 = getDetailHeightSample(pos + t.zyy,basenormal,detailnormal,param,scale).y;
	float h3 = getDetailHeightSample(pos + t.yyx,basenormal,detailnormal,param,scale).y;
	float h4 = getDetailHeightSample(pos + t.yyz,basenormal,detailnormal,param,scale).y;

	res.materialdisp = h0;
	res.normal = normalize(vec3(h2-h1,8.0 * t_,h4-h3)) * surfaceBasis;
	getMaterial(h0.x, pos, basenormal, res.normal, param, AO, noiseSample, res.diffuse, res.shading);

	return res;
}
/*
DetailSample sampleDetail(vec2 pos, vec3 basenormal, vec3 detailnormal, vec4 param, float AO, vec2 scale, float t_, mat3 surfaceBasis)
{
	return sampleDetail(vec3(pos.x,0.0,pos.y),basenormal,detailnormal,param,AO,scale,t_,surfaceBasis);
}*/

DetailSample sampleDetailLow(vec3 pos, vec3 basenormal, vec3 detailnormal, vec4 param, float AO, vec2 scale, float t_)
{
	DetailSample res;
	vec3 t = vec3(-t_,0.0,t_);

	vec4 noiseSample = vec4(0.5);
	vec2 h0 = getDetailHeightSample(pos,basenormal,detailnormal,param,scale, noiseSample);

	res.materialdisp = h0;
	res.normal = basenormal;
	getMaterial(h0.x, pos, basenormal, detailnormal, param, AO, noiseSample, res.diffuse, res.shading);

	return res;
}



//|FragmentCommon
float getDetailBias()
{
	return highDetailBlend;
}



//|CubicHeightSample
// 4-tap b-spline bicubic interpolation.
// credit to http://vec3.ca/bicubic-filtering-in-fewer-taps/
float sampleHeight(vec2 pos, float scale)
{
	// get texel centre
	vec2 tc = pos * vec2(scale);
	vec2 itc = floor(tc);
	
	// fractional offset
	vec2 f = tc - itc;

	vec2 f2 = f*f;
	vec2 f3 = f2*f;

	// bspline weights
	vec2 w0 = f2 - (f3 + f) * 0.5;
    vec2 w1 = f3 * 1.5 - f2 * 2.5 + vec2(1.0);
    vec2 w3 = (f3 - f2) * 0.5;
    vec2 w2 = vec2(1.0) - w0 - w1 - w3;

	vec2 s0 = w0 + w1;
	vec2 s1 = w2 + w3;

	vec2 f0 = w1 / (w0 + w1);
	vec2 f1 = w3 / (w2 + w3);

	vec2 t0 = (itc - vec2(1.0) + f0) * t;
	vec2 t1 = (itc + vec2(1.0) + f1) * t;

	return 
		textureLod(heightTex,vec2(t0.x,t0.y),0).r * s0.x * s0.y +
		textureLod(heightTex,vec2(t1.x,t0.y),0).r * s1.x * s0.y +
		textureLod(heightTex,vec2(t0.x,t1.y),0).r * s0.x * s1.y +
		textureLod(heightTex,vec2(t1.x,t1.y),0).r * s1.x * s1.y;
}

//|CubicParamSample
// 4-tap b-spline bicubic interpolation.
// credit to http://vec3.ca/bicubic-filtering-in-fewer-taps/
vec4 sampleParam(vec2 pos, float scale)
{
	// get texel centre
	vec2 tc = pos * vec2(scale);
	vec2 itc = floor(tc);
	
	// fractional offset
	vec2 f = tc - itc;

	vec2 f2 = f*f;
	vec2 f3 = f2*f;

	// bspline weights
	vec2 w0 = f2 - (f3 + f) * 0.5;
    vec2 w1 = f3 * 1.5 - f2 * 2.5 + vec2(1.0);
    vec2 w3 = (f3 - f2) * 0.5;
    vec2 w2 = vec2(1.0) - w0 - w1 - w3;

	vec2 s0 = w0 + w1;
	vec2 s1 = w2 + w3;

	vec2 f0 = w1 / (w0 + w1);
	vec2 f1 = w3 / (w2 + w3);

	vec2 t0 = (itc - vec2(1.0) + f0) * t;
	vec2 t1 = (itc + vec2(1.0) + f1) * t;

	return 
		textureLod(paramTex,vec2(t0.x,t0.y),0) * s0.x * s0.y +
		textureLod(paramTex,vec2(t1.x,t0.y),0) * s1.x * s0.y +
		textureLod(paramTex,vec2(t0.x,t1.y),0) * s0.x * s1.y +
		textureLod(paramTex,vec2(t1.x,t1.y),0) * s1.x * s1.y;
}


//|CubicNormalSample
vec3 getNormal(vec2 pos, float scale)
{
	// get texel centre
	vec2 tc = pos * vec2(scale);
	vec2 itc = floor(tc);
	
	// fractional offset
	vec2 f = tc - itc;

	vec2 f2 = f*f;
	vec2 f3 = f2*f;

	// bspline weights 
	vec2 w0 = f2 - (f3 + f) * 0.5;
    vec2 w1 = f3 * 1.5 - f2 * 2.5 + vec2(1.0);
    vec2 w3 = (f3 - f2) * 0.5;
    vec2 w2 = vec2(1.0) - w0 - w1 - w3;

	vec2 s0 = w0 + w1;
	vec2 s1 = w2 + w3;

	vec2 f0 = w1 / (w0 + w1);
	vec2 f1 = w3 / (w2 + w3);

	vec2 t0 = (itc - vec2(1.0) + f0) * t;
	vec2 t1 = (itc + vec2(1.0) + f1) * t;

	return normalize(
		(
			textureLod(normalTex,vec2(t0.x,t0.y),0).rgb * s0.x * s0.y +
			textureLod(normalTex,vec2(t1.x,t0.y),0).rgb * s1.x * s0.y +
			textureLod(normalTex,vec2(t0.x,t1.y),0).rgb * s0.x * s1.y +
			textureLod(normalTex,vec2(t1.x,t1.y),0).rgb * s1.x * s1.y
		) 
		);
}



//|LowVertex
#version 140
 
uniform sampler2D heightTex;
uniform sampler2D detailTex;

uniform mat4 transform_matrix;
uniform vec4 boxparam;
uniform vec3 eyePos;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailTexScale; 

in vec3 vertex;
in vec3 in_boxcoord;

out vec3 boxcoord;
out vec2 texcoord;
out vec2 detailcoord;
out float highDetailBlend;

#include ".|Common"



float getHeight(vec2 pos)
{
	return textureLod(heightTex,pos,0).r;
}

float sampleHeight(vec2 pos)
{
	float c = getHeight(pos);
	return c;
}

void main() {

	vec3 b = in_boxcoord;
	b.xz *= scale;
	b.xz += offset;
	texcoord = b.xz;

	detailcoord = getDetailTexcoord(texcoord);

	float h = sampleHeight(texcoord);

	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h + v.y;

	vec4 tv = transform_matrix * vec4(v, 1.0);
	highDetailBlend = getHighDetailBlendFactor(tv);
    gl_Position = tv;

	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;


}

//|LowFragment
#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D paramTex;
uniform sampler2D shadeTex;
uniform sampler2D detailTex;
uniform vec4 boxparam;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailTexScale; 

in vec3 boxcoord;
in vec2 texcoord;
in vec2 detailcoord;
in float highDetailBlend;

out vec4 out_Colour;
out vec4 out_Normal;
out vec4 out_Shading;
out vec4 out_Lighting;

#include ".|Common"
#include ".|FragmentCommon"

void main(void)
{
	vec2 shadowAO = texture(shadeTex,texcoord).rg;
	float height = textureLod(heightTex,texcoord,0).r;
	vec4 param = texture2D(paramTex,texcoord);

	vec3 normal = texture(normalTex,texcoord).rgb;
	DetailSample detail;

	//if (highDetailBlend <= 0.0){
		detail = sampleDetailLow(vec3(detailcoord.x, height, detailcoord.y), normal, normal, param, shadowAO.g, detailScale, detailSampleOffset);
	//}
	//else{
	//	detail = sampleDetail(vec3(detailcoord.x,0.0,detailcoord.y), normal, normal, param, detailScale, detailSampleOffset);
	//}

    out_Normal = vec4(normal,0.0);

	out_Colour = detail.diffuse; 
	out_Shading = detail.shading;

	float shadow = SmoothShadow((height + normal.y * detail.materialdisp.y) - shadowAO.r);

	out_Lighting = vec4(shadow,shadowAO.g,0.0,0.0);

}

//|MediumVertex
#version 140
 
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D detailTex;

uniform mat4 transform_matrix;
uniform vec4 boxparam;
uniform vec3 eyePos;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailTexScale; 

in vec3 vertex;
in vec3 in_boxcoord;

out vec3 basevertex;
out vec3 normal;
out vec3 binormal;
out vec3 tangent;
out vec3 boxcoord;
out vec2 texcoord;
out vec2 detailcoord;
out float highDetailBlend;

#include ".|Common"


float getHeight(vec2 pos)
{
	return textureLod(heightTex,pos,0).r;
}

float sampleHeight(vec2 pos)
{
	float c = getHeight(pos);
	return c;
}

void main() {

	vec3 b = in_boxcoord;
	b.xz *= scale;
	b.xz += offset;
	texcoord = b.xz;
	detailcoord = getDetailTexcoord(texcoord);

	float h = sampleHeight(texcoord);
	normal = textureLod(normalTex,texcoord,0).rgb;
	// calculate tangent and binormal
	// tangent is in X direction, so is the cross product of normal (Y) and Z
	vec3 t1 = normalize(cross(normal,vec3(0.0,0.0,-1.0)));
	binormal = normalize(cross(t1,normal));
	tangent = normalize(cross(normal,binormal));

	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h + v.y;

	basevertex = v;
	vec4 tv = transform_matrix * vec4(v, 1.0);
	highDetailBlend = getHighDetailBlendFactor(tv);
    gl_Position = tv;


	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;

}

//|MediumFragment
#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D paramTex;
uniform sampler2D shadeTex;
uniform sampler2D detailTex;
uniform vec4 boxparam;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailTexScale; 

in vec3 basevertex;
in vec3 normal;
in vec3 binormal;
in vec3 tangent;
in vec3 boxcoord;
in vec2 texcoord;
in vec2 detailcoord;
in float highDetailBlend;

out vec4 out_Colour;
out vec4 out_Normal;
out vec4 out_Shading;
out vec4 out_Lighting;

#include ".|Common"
#include ".|FragmentCommon"

void main(void)
{
	vec2 shadowAO = texture(shadeTex,texcoord).rg;
	float height = textureLod(heightTex,texcoord,0).r;


   	vec4 param = texture2D(paramTex,texcoord);

	float detailBias = getDetailBias();

	// calculate normal of detail heightmap at detailpos
	mat3 nm = mat3(tangent,normal,binormal);

	// sample detail
	DetailSample detail = sampleDetail(vec3(detailcoord.x, height, detailcoord.y), normal, normal, param, shadowAO.g, detailScale, detailSampleOffset, nm);

	//detail.normal = mix(vec3(0.0,1.0,0.0),detail.normal,detailBias);


	//vec3 n = normalize(detail.normal * nm);
	
    out_Normal = vec4(detail.normal ,1.0);


	float shadow = SmoothShadow((height + normal.y * detail.materialdisp.y) - shadowAO.r);

	out_Colour = detail.diffuse; 
	out_Shading = detail.shading;
	out_Lighting = vec4(shadow,shadowAO.g,0.0,0.0);

}


//|HighVertex
#version 140
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D paramTex;
uniform sampler2D detailTex;

uniform mat4 transform_matrix;
uniform vec4 boxparam;
uniform vec3 eyePos;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailTexScale; 

in vec3 vertex;
in vec3 in_boxcoord;

out vec3 basevertex;
out vec3 boxcoord;
out vec3 normal;
out vec3 binormal;
out vec3 tangent;
out vec2 texcoord;
out vec2 detailcoord;
out float highDetailBlend;

#include ".|Common"


float getHeight(vec2 pos)
{
	return textureLod(heightTex,pos,0).r;
}

float sampleHeight(vec2 pos)
{
	float c = getHeight(pos);
	return c;
}

#include ".|CubicHeightSample"
#include ".|CubicParamSample"
#include ".|CubicNormalSample"

void main() {

	vec3 b = in_boxcoord;
	b.xz *= scale;
	b.xz += offset;
	texcoord = b.xz;
	detailcoord = getDetailTexcoord(texcoord);

	float h = sampleHeight(texcoord,boxparam.x);
	normal = getNormal(texcoord,boxparam.x);
	vec4 param = sampleParam(texcoord,boxparam.x);

	// calculate tangent and binormal
	// tangent is in X direction, so is the cross product of normal (Y) and Z
	vec3 t1 = normalize(cross(normal,vec3(0.0,0.0,-1.0)));
	binormal = normalize(cross(t1,normal));
	tangent = normalize(cross(normal,binormal));


	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h + v.y;

	basevertex = v;

	// get detail
	vec2 detail = getDetailHeightSample(vec3(detailcoord.x, h, detailcoord.y), normal, normal, param, vec2(detailScale));
	
	v += normal * detail.y;

	vec4 tv = transform_matrix * vec4(v, 1.0);
	highDetailBlend = getHighDetailBlendFactor(tv);
    gl_Position = tv;


	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;

}

//|HighFragment
#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D paramTex;
uniform sampler2D shadeTex;
uniform sampler2D detailTex;
uniform vec4 boxparam;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailTexScale; 

in vec3 basevertex;
in vec3 boxcoord;
in vec3 normal;
in vec3 binormal;
in vec3 tangent;
in vec2 texcoord;
in vec2 detailcoord;
in float highDetailBlend;

out vec4 out_Colour;
out vec4 out_Normal;
out vec4 out_Shading;
out vec4 out_Lighting;

#include ".|Common"
#include ".|FragmentCommon"

float getDUV()
{
	vec2 duv = abs(fwidth(texcoord));
	return duv.x + duv.y;
}


void main(void)
{
	vec2 shadowAO = texture(shadeTex,texcoord).rg;
	vec4 param = texture2D(paramTex,texcoord);
	float detailBias = getDetailBias();

	float height = boxcoord.y;//textureLod(heightTex,texcoord,0).r;


	// get screen-space derivative
	vec2 duv = abs(fwidth(texcoord));
	duv.x = max(minduv,duv.x + duv.y);

	// generate transform matrix for normals
	mat3 nm = mat3(tangent,normal,binormal);

	vec3 bedrockNormal = getDetailBedrockNormal(detailcoord, detailSampleOffset);
	bedrockNormal = normalize(bedrockNormal * nm);

	// sample detail
	DetailSample detail = sampleDetail(vec3(detailcoord.x, height, detailcoord.y), normal, bedrockNormal, param, shadowAO.g, detailScale, detailSampleOffset, nm);
	
	detail.normal = mix(vec3(0.0,1.0,0.0),detail.normal,detailBias);

	// calculate normal of detail heightmap at detailpos
	//vec3 n = normalize(detail.normal * nm);
	
	out_Colour = detail.diffuse; 
	out_Shading = detail.shading;
    out_Normal = vec4(detail.normal,1.0);

	float shadow = SmoothShadow((height + normal.y * detail.materialdisp.y) - shadowAO.r);

	out_Lighting = vec4(shadow,shadowAO.g,0.0,0.0);

}


