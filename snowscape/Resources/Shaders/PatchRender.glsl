﻿//|Common

float t = 1.0 / boxparam.x;
const vec2 detailScale = vec2(0.2,0.2);  // detailTexScale is uniform
const float sampleOfs = 1.0/1024.0;
const float minduv = 0.0001;

float SmoothShadow(float heightdiff)
{
	return smoothstep(-0.5,-0.02,heightdiff);
}

#include "noise2d.glsl"

float sfbm(vec2 pos)
{
	float a = snoise(pos); 
	a += snoise(pos * 2.0) * 0.5; 
	a += snoise(pos * 4.0) * 0.25; 
	a += snoise(pos * 8.0) * 0.125; 
	return a;
}

// gets a tuple of material (x) and displacement (y) for a given point.
//
// todo: implement as material stack
//
// pos: base terrain pos (bicubic interpolation of base terrain) - used as noise source coordinate
// basenormal: base terrain normal (bicubic interpolation of base terrain) - used for slope
// param: thickness of terrain layers
// scale: resolution (x) and height (y) of detail
vec2 getDetailHeightSample(vec3 pos, vec3 basenormal, vec4 param, vec2 scale)
{
	float displacement = sfbm(pos.xz * scale.x) * scale.y;
	displacement += snoise(pos.xz * scale.x * 64.0) * scale.y * 0.01;
	displacement += snoise(pos.xz * scale.x * 256.0) * scale.y * 0.001;

	return vec2(0.0,displacement);
}

struct DetailSample
{
	vec3 normal;
	vec2 materialdisp;
};

DetailSample sampleDetail(vec3 pos, vec3 basenormal, vec4 param, vec2 scale, float t_)
{
	DetailSample res;
	vec3 t = vec3(-t_,0.0,t_);

	vec2 h0 = getDetailHeightSample(pos,basenormal,param,scale);

	//   3
	// 1 0 2
	//   4
	// get adjacent samples
	// todo: approximate by using the same basenormal & param. Ideally these should be fetched from the texture again.
	float h1 = getDetailHeightSample(pos + t.xyy,basenormal,param,scale).y;
	float h2 = getDetailHeightSample(pos + t.zyy,basenormal,param,scale).y;
	float h3 = getDetailHeightSample(pos + t.yyx,basenormal,param,scale).y;
	float h4 = getDetailHeightSample(pos + t.yyz,basenormal,param,scale).y;

	res.materialdisp = h0;
	res.normal = normalize(vec3(h2-h1,2.0 * t_,h4-h3));

	return res;
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

	float h = sampleHeight(texcoord);

	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h + v.y;

    gl_Position = transform_matrix * vec4(v, 1.0);

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
uniform vec4 boxparam;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailTexScale; 

in vec3 boxcoord;
in vec2 texcoord;

out vec4 out_Colour;
out vec4 out_Normal;
out vec4 out_Shading;
out vec4 out_Lighting;

#include ".|Common"

void main(void)
{
	vec2 shadowAO = texture(shadeTex,texcoord).rg;
	float height = textureLod(heightTex,texcoord,0).r;

	out_Colour = vec4(0.2,0.2,0.6,0.1);
    out_Normal = texture(normalTex,texcoord);

	float shadow = SmoothShadow(height - shadowAO.r);

	out_Shading = vec4(0.0);
	out_Lighting = vec4(shadow,shadowAO.g,0.0,0.0);

}

//|MediumVertex
#version 140
 
uniform sampler2D heightTex;

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
out vec2 texcoord;

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

	float h = sampleHeight(texcoord);

	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h + v.y;

	basevertex = v;

    gl_Position = transform_matrix * vec4(v, 1.0);

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
uniform vec4 boxparam;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailTexScale; 

in vec3 basevertex;
in vec3 boxcoord;
in vec2 texcoord;

out vec4 out_Colour;
out vec4 out_Normal;
out vec4 out_Shading;
out vec4 out_Lighting;

#include ".|Common"
#include ".|CubicNormalSample"

void main(void)
{
	vec2 shadowAO = texture(shadeTex,texcoord).rg;
	float height = textureLod(heightTex,texcoord,0).r;


   	vec4 param = texture2D(paramTex,texcoord);
	//out_Normal = texture(normalTex,texcoord);

	// get bicubic interpolated normal
	vec3 normal = getNormal(texcoord,boxparam.x);

	// calculate tangent and binormal
	// tangent is in X direction, so is the cross product of normal (Y) and Z
	vec3 t1 = normalize(cross(normal,vec3(0.0,0.0,-1.0)));
	vec3 binormal = normalize(cross(t1,normal));
	vec3 tangent = normalize(cross(normal,binormal));

	// get screen-space derivative
	vec2 duv = abs(fwidth(texcoord));
	duv.x = max(minduv,duv.x + duv.y);

	// sample detail
	DetailSample detail = sampleDetail(basevertex, normal, param, detailScale, duv.x);

	// calculate normal of detail heightmap at detailpos
	mat3 nm = mat3(tangent,normal,binormal);
	//vec3 dn = vec3(0.0,1.0,0.0);//getDetailNormal();
	vec3 n = normalize(detail.normal * nm);
	
    out_Normal = vec4(n,1.0);


	float shadow = SmoothShadow(height - shadowAO.r);

	out_Colour = vec4(0.2,0.6,0.2,0.1);
	out_Shading = vec4(0.0);
	out_Lighting = vec4(shadow,shadowAO.g,0.0,0.0);

}


//|HighVertex
#version 140
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D paramTex;

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

#include ".|CubicNormalSample"

void main() {

	vec3 b = in_boxcoord;
	b.xz *= scale;
	b.xz += offset;
	texcoord = b.xz;

	float h = sampleHeight(texcoord,boxparam.x);
	normal = getNormal(texcoord,boxparam.x);
	vec4 param = texture2D(paramTex,texcoord);

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

	// todo: add displacement in direction of normal
	// get detail
	vec2 detail = getDetailHeightSample(v, normal, param, detailScale);
	
	v += normal * detail.y;

    gl_Position = transform_matrix * vec4(v, 1.0);

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

out vec4 out_Colour;
out vec4 out_Normal;
out vec4 out_Shading;
out vec4 out_Lighting;

#include ".|Common"

void main(void)
{
	vec2 shadowAO = texture(shadeTex,texcoord).rg;
	vec4 param = texture2D(paramTex,texcoord);

	float height = boxcoord.y;//textureLod(heightTex,texcoord,0).r;

	out_Colour = vec4(0.6,0.2,0.2,0.1);

	// get screen-space derivative
	vec2 duv = abs(fwidth(texcoord));
	duv.x = max(minduv,duv.x + duv.y);

	// sample detail
	DetailSample detail = sampleDetail(basevertex, normal, param, detailScale, duv.x);

	// calculate normal of detail heightmap at detailpos
	mat3 nm = mat3(tangent,normal,binormal);
	//vec3 dn = vec3(0.0,1.0,0.0);//getDetailNormal();
	vec3 n = normalize(detail.normal * nm);
	
    out_Normal = vec4(n,1.0);

	float shadow = SmoothShadow(height - shadowAO.r);

	out_Shading = vec4(0.0);
	out_Lighting = vec4(shadow,shadowAO.g,0.0,0.0);

}


