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
uniform float mieBrightness;
uniform float miePhase;// = 0.97;
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
