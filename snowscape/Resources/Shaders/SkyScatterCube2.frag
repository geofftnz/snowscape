#version 140
precision highp float;

uniform vec3 sunVector;
uniform float groundLevel;

uniform float raleighBrightness;
uniform float mieBrightness;
uniform float scatterAbsorb;
uniform vec3 Kr;
uniform vec3 eye;
uniform vec3 sunLight;

uniform vec3 facenormal;
uniform vec3 facexbasis;
uniform vec3 faceybasis;

in vec2 sky2d;

out vec3 out_Sky;


//float groundHeight = 0.995;
//float eyeHeight = 0.996;
float earthAtmosphereRadius = 6450.0;
//float mieBrightness = 0.02;
//float ralBrightness = 1.0;
float miePhase = 0.97;
float ralPhase = -0.01;
//

#include "atmospheric.glsl"


//vec3 Kr = vec3(2.284, 3.897, 8.227) * 0.11;
//vec3 Kr = vec3(0.1287, 0.2698, 0.7216);
//vec3 Kr = vec3(0.1,0.3,0.98);

//vec3 sunLight = vec3(1.0)*20.0;



void main(void)
{
	vec3 eye2 = vec3(0.0,groundLevel + 2.0 / earthAtmosphereRadius ,0.0);
	vec3 dir = normalize(facenormal + facexbasis * sky2d.x + faceybasis * sky2d.y);

	//out_Sky = getRayMarchedScattering(eye, dir, sunVector, scatterAbsorb,0.0,100000.0);
	out_Sky = getSimpleScattering(eye2, dir, sunVector, scatterAbsorb,10000.0);
}
