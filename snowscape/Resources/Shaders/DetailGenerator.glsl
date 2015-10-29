
//|CubicHeightSample
// 4-tap b-spline bicubic interpolation.
// credit to http://vec3.ca/bicubic-filtering-in-fewer-taps/
float sampleHeightBicubic(vec2 pos, float scale)
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
		texture(heightTex,vec2(t0.x,t0.y)).r * s0.x * s0.y +
		texture(heightTex,vec2(t1.x,t0.y)).r * s1.x * s0.y +
		texture(heightTex,vec2(t0.x,t1.y)).r * s0.x * s1.y +
		texture(heightTex,vec2(t1.x,t1.y)).r * s1.x * s1.y;
}

//|CubicParamSample
// 4-tap b-spline bicubic interpolation.
// credit to http://vec3.ca/bicubic-filtering-in-fewer-taps/
vec4 sampleParamBicubic(vec2 pos, float scale)
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


//|vert
#version 140
precision highp float;
in vec3 vertex;
out vec2 texcoord;

uniform float invtexsize;
uniform vec2 position;
const float scale = 16.0;
const float invscale = 1.0 / 16.0;

void main() {

	gl_Position = vec4(vertex.xy,0.0,1.0);
	texcoord = (vertex.xy * 0.5) * invscale + position * invtexsize;
}

//|frag
#version 140
precision highp float;

uniform sampler2D heightTex;
uniform sampler2D paramTex;
uniform float texsize;
uniform float invtexsize;


in vec2 texcoord;

out float out_Height;
out vec4 out_Normal;
out vec4 out_Param;

float t = invtexsize;

#include ".|CubicHeightSample"
#include ".|CubicParamSample"
#include "noise2d.glsl"


float sampleHeight(vec2 pos)
{
	float h0 = sampleHeightBicubic(pos,texsize);

	float n = 0.5;
	h0 += snoise(pos*256.0) * n; n*=0.5;
	h0 += snoise(pos*512.0) * n; n*=0.5;
	h0 += snoise(pos*1024.0) * n; n*=0.5;

	return h0;
}

vec3 getNormal(vec2 pos)
{
    float h1 = sampleHeight(vec2(pos.x, pos.y - t));
	float h2 = sampleHeight(vec2(pos.x, pos.y + t));
    float h3 = sampleHeight(vec2(pos.x - t, pos.y));
	float h4 = sampleHeight(vec2(pos.x + t, pos.y));
	return normalize(vec3(h3-h4,2.0,h1-h2));
}

void main(void)
{
	out_Height = sampleHeight(texcoord);
	out_Normal = vec4(getNormal(texcoord),1.0);
	out_Param = textureLod(paramTex,texcoord,0);
}
