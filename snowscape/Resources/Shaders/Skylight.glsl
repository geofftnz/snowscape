//|vs
#version 140

in vec3 vertex;
out vec2 sky2d;

void main() {

	gl_Position = vec4(vertex.xy,0.0,1.0);
	sky2d = vertex.xy;
}

//|fs
#version 140
precision highp float;

uniform vec3 sunVector;
uniform float groundLevel;
uniform float rayleighBrightness;
float mieBrightness = 0.1;
float miePhase = 0.7;// = 0.97;
uniform float rayleighPhase;// = -0.01;
uniform float skyPrecalcBoundary;  // 16

uniform float scatterAbsorb;
uniform vec3 Kr;
uniform vec3 eye;
uniform vec3 sunLight;

in vec2 sky2d;

out vec3 out_Sky;

// expected variables for atmospheric scattering
float earthAtmosphereRadius = 6450.0;


#include "atmospheric.glsl"


void main(void)
{

	// let's assume we're looking down the z axis
	// screen will be x,y

	// we're in a cube of radius 1.0
	// assume eye is at 0,0,0

	// set up boundary between near and far scattering
	float boundary = skyPrecalcBoundary / earthAtmosphereRadius;  // 4km

	vec2 p2 = sky2d;

	//out_Sky = vec3(0.0);

	if (dot(p2,p2) > 1.0)
	{
		p2 = normalize(p2);
	}

	vec3 dir = vec3(p2.x, 1.0 - length(p2), p2.y);

	out_Sky = getRayMarchedScattering(vec3(0.0,groundLevel+0.001,0.0), dir, sunVector, scatterAbsorb, 0.0,10000.0);

	// debug
	//out_Sky.r = max(0.0,1.0 - length(p2)*4.0);
}

//|blursample
vec3 sampleBlur(vec2 p, vec2 ofs, float weight)
{
	// offset p by ofs, but keep within unit circle. assume p in -1..1

	vec2 q = p + ofs;
	float d = length(q);

	if (d>1.0){
		q = normalize(q);
		weight *= (1.0 / (1.0 + (d - 1.0) * 2.0 ));
	}

	return texture(skyTex,q * 0.5 + 0.5).rgb * weight;
}

vec3 blur(vec2 p,float s)
{
	vec3 c = vec3(0.0);

	c+=sampleBlur(p,vec2(-3.0,-3.0)*s,0.000036);	c+=sampleBlur(p,vec2(-2.0,-3.0)*s,0.000366);	c+=sampleBlur(p,vec2(-1.0,-3.0)*s,0.001452);	c+=sampleBlur(p,vec2(0.0,-3.0)*s,0.002298);	c+=sampleBlur(p,vec2(1.0,-3.0)*s,0.001452);	c+=sampleBlur(p,vec2(2.0,-3.0)*s,0.000366);	c+=sampleBlur(p,vec2(3.0,-3.0)*s,0.000036);
	c+=sampleBlur(p,vec2(-3.0,-2.0)*s,0.000366);	c+=sampleBlur(p,vec2(-2.0,-2.0)*s,0.003721);	c+=sampleBlur(p,vec2(-1.0,-2.0)*s,0.014762);	c+=sampleBlur(p,vec2(0.0,-2.0)*s,0.023363);	c+=sampleBlur(p,vec2(1.0,-2.0)*s,0.014762);	c+=sampleBlur(p,vec2(2.0,-2.0)*s,0.003721);	c+=sampleBlur(p,vec2(3.0,-2.0)*s,0.000366);
	c+=sampleBlur(p,vec2(-3.0,-1.0)*s,0.001452);	c+=sampleBlur(p,vec2(-2.0,-1.0)*s,0.014762);	c+=sampleBlur(p,vec2(-1.0,-1.0)*s,0.058564);	c+=sampleBlur(p,vec2(0.0,-1.0)*s,0.092686);	c+=sampleBlur(p,vec2(1.0,-1.0)*s,0.058564);	c+=sampleBlur(p,vec2(2.0,-1.0)*s,0.014762);	c+=sampleBlur(p,vec2(3.0,-1.0)*s,0.001452);
	c+=sampleBlur(p,vec2(-3.0,0.0)*s,0.002298);	c+=sampleBlur(p,vec2(-2.0,0.0)*s,0.023363);	c+=sampleBlur(p,vec2(-1.0,0.0)*s,0.092686);	c+=sampleBlur(p,vec2(0.0,0.0)*s,0.146689);	c+=sampleBlur(p,vec2(1.0,0.0)*s,0.092686);	c+=sampleBlur(p,vec2(2.0,0.0)*s,0.023363);	c+=sampleBlur(p,vec2(3.0,0.0)*s,0.002298);
	c+=sampleBlur(p,vec2(-3.0,1.0)*s,0.001452);	c+=sampleBlur(p,vec2(-2.0,1.0)*s,0.014762);	c+=sampleBlur(p,vec2(-1.0,1.0)*s,0.058564);	c+=sampleBlur(p,vec2(0.0,1.0)*s,0.092686);	c+=sampleBlur(p,vec2(1.0,1.0)*s,0.058564);	c+=sampleBlur(p,vec2(2.0,1.0)*s,0.014762);	c+=sampleBlur(p,vec2(3.0,1.0)*s,0.001452);
	c+=sampleBlur(p,vec2(-3.0,2.0)*s,0.000366);	c+=sampleBlur(p,vec2(-2.0,2.0)*s,0.003721);	c+=sampleBlur(p,vec2(-1.0,2.0)*s,0.014762);	c+=sampleBlur(p,vec2(0.0,2.0)*s,0.023363);	c+=sampleBlur(p,vec2(1.0,2.0)*s,0.014762);	c+=sampleBlur(p,vec2(2.0,2.0)*s,0.003721);	c+=sampleBlur(p,vec2(3.0,2.0)*s,0.000366);
	c+=sampleBlur(p,vec2(-3.0,3.0)*s,0.000036);	c+=sampleBlur(p,vec2(-2.0,3.0)*s,0.000366);	c+=sampleBlur(p,vec2(-1.0,3.0)*s,0.001452);	c+=sampleBlur(p,vec2(0.0,3.0)*s,0.002298);	c+=sampleBlur(p,vec2(1.0,3.0)*s,0.001452);	c+=sampleBlur(p,vec2(2.0,3.0)*s,0.000366);	c+=sampleBlur(p,vec2(3.0,3.0)*s,0.000036);

	return c;
}


//|blur1
#version 140
precision highp float;

uniform sampler2D skyTex;
in vec2 sky2d;
out vec3 out_Sky;

#include "Skylight.glsl|blursample"



void main(void)
{
	out_Sky = blur(sky2d,0.05) * 0.5;
	out_Sky += blur(sky2d,0.17) * 0.5;
}

//|blur2

#version 140
precision highp float;

uniform sampler2D skyTex;
in vec2 sky2d;
out vec3 out_Sky;

#include "Skylight.glsl|blursample"

void main(void)
{
	out_Sky = blur(sky2d,0.21) * 0.5;
	out_Sky += blur(sky2d,0.33) * 0.5;
}



