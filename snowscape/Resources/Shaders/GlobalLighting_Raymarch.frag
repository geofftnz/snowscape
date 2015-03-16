﻿// colour: rgb, material
// normal+?
// shading: [roughness|reflection], specexp, specpwr, sparkle
// lighting - shadow, AO, emmissive, subsurface

#version 140
precision highp float;

// textures from gbuffer
uniform sampler2D colourTex;  // 0
uniform sampler2D normalTex;   // 1
uniform sampler2D shadingTex;    // 2
uniform sampler2D lightingTex;    // 3

// global textures
uniform sampler2D heightTex;
uniform sampler2D shadeTex;
uniform sampler2D indirectTex;
uniform sampler2D depthTex;
uniform sampler2D skylightSharpTex;
uniform sampler2D skylightSmoothTex;

uniform samplerCube skyCubeTex;

// projection
uniform mat4 pre_projection_matrix;

uniform vec4 boxparam;
uniform vec3 eyePos;
uniform vec3 sunVector;

uniform float groundLevel;
uniform float rayleighBrightness;
uniform float mieBrightness;
uniform float miePhase;// = 0.97;
uniform float rayleighPhase;// = -0.01;
uniform float skyPrecalcBoundary;  // 16

uniform float scatterAbsorb;
uniform vec3 Kr;
//uniform vec3 eye;
uniform vec3 sunLight;

const float tileSizeKm = 16.0;

/*
uniform float minHeight;
uniform float maxHeight;
uniform float exposure;
uniform float scatterAbsorb;
uniform vec3 Kr;
uniform float raleighBrightness;
uniform float mieBrightness;
uniform float miePhase;
uniform float nearMieBrightness;
uniform float skylightBrightness;
uniform float groundLevel;
uniform vec3 sunLight;
uniform float sampleDistanceFactor; // convert terrain coordinates into sky-scattering coordinates for absorb(). Started at 0.004/6000.0 to match skybox
uniform float aoInfluenceHeight; // height above the terrain to blend influence of AO when raymarching scattering
uniform float time;
uniform float nearScatterDistance;
uniform float ambientBias;  // amount of skylight
uniform float indirectBias; // amount of indirect light
uniform float renderMode;

// scattering performance
uniform float scatteringInitialStepSize;
uniform float scatteringStepGrowthFactor;

uniform float snowSlopeDepthAdjust;
uniform float normalBlendNearDistance;
uniform float normalBlendFarDistance;
*/

in vec2 texcoord0;
out vec4 out_Colour;

float texel = 1.0 / boxparam.x;

// expected variables for atmospheric scattering
float earthAtmosphereRadius = 6450.0;
#include "atmospheric.glsl"



void main(void)
{
// colour: rgb, material
// normal+?
// shading: [roughness|reflection], specexp, specpwr, sparkle
// lighting - shadow, AO, emmissive, subsurface


	vec4 colourT = texture(colourTex,texcoord0.xy);
    vec4 normalT = texture(normalTex,texcoord0.xy);
    vec4 shadingT = texture(shadingTex,texcoord0.xy);
    vec4 lightingT = texture(lightingTex,texcoord0.xy);
	float depth = texture(depthTex,texcoord0.xy).r;

	vec4 projpos = vec4(texcoord0.x * 2.0 - 1.0, texcoord0.y * 2.0 - 1.0, depth*2.0-1.0, 1.0);
	vec4 pos = pre_projection_matrix * projpos;
	pos.xyz /= pos.w;

	vec3 dir = pos.xyz-eyePos.xyz;
	float len = length(dir);
	dir = normalize(dir);

	float ndist = len * tileSizeKm * texel;

    vec3 c = vec3(0.0,0.0,0.0);
    
	vec3 normal = normalT.rgb;
	vec3 refl = reflect(dir,normal);

    //vec3 normal = normalize(normalT.xyz - vec3(0.5));
    vec2 shadowAO = texture(shadeTex,pos.xz * texel).rg;

	float eyeShadow = smoothstep(-0.05,0.05,eyePos.y - texture(shadeTex,eyePos.xz * texel).r);

	// output diffuse
	//c = diffuseT.rgb;
	//c = colourT.rgb;

	//c.rg = lightingT.rg;
	//c.rgb = normalT.rgb;

	// debug: lighting components only
	//colourT.rgb = colourT.a > 0.9999 ? colourT.rgb:vec3(1.0);


	float roughness = max(0.0,(shadingT.r-0.5)) * 2.0;  
	float skyreflection = clamp(0.5-shadingT.r,0.0,0.5) * 2.0;

	//vec3 ambientlight = vec3(0.7,0.8,1.0) * 0.3;

	vec3 skyReflectionEnv = texture(skyCubeTex,refl.xyz).rgb; //texture(skylightSharpTex,refl.xz * 0.5 + 0.5).rgb * 0.5;
	// texture(skyCubeTex,refl.xyz).rgb

	vec3 ambientDirection = mix(vec3(0.0,1.0,0.0),normal,0.4 + 0.6 * lightingT.g*lightingT.g); // skew ambient normal towards up-vector where there is more ambient occlusion
	vec3 ambientlight = texture(skylightSmoothTex,ambientDirection.xz * 0.5 + 0.5).rgb * (1.0 - shadingT.b);

	vec3 ambientDirectionRefl = mix(vec3(0.0,1.0,0.0),refl,0.4 + 0.6 * lightingT.g*lightingT.g); // skew ambient normal towards up-vector where there is more ambient occlusion
	ambientlight += texture(skylightSmoothTex,ambientDirectionRefl.xz * 0.5 + 0.5).rgb * shadingT.b;


	//vec3 sunlight = vec3(1.0,0.95,0.92) * 5.0;

	vec3 sunAtP = getSunInflux(vec3(0.0),sunVector);

	vec3 sun = (sunAtP * lightingT.r) * clamp(dot(normal, sunVector),0.0,1.0);  // diffuse sun - needs to be oren-nayar + specular
	vec3 spec = (sunAtP * lightingT.r) * clamp(pow(dot(refl, sunVector),shadingT.g*100.0),0.0,1.0) * shadingT.b;

	vec3 ambient = ambientlight * lightingT.g;


	c = colourT.rgb * (sun + ambient + vec3(lightingT.b)) + spec;


	// add sky reflection
	c +=  skyReflectionEnv * skyreflection * lightingT.g;


	//c = texture(skyCubeTex,normal).rgb;

	// incoming scattering

	//float distnorm = min(len, skyPrecalcBoundary)  / earthAtmosphereRadius;  // len  * 0.256

	float eyeHeightNorm = max(0.0,((eyePos.y * tileSizeKm * texel) / earthAtmosphereRadius)) + groundLevel;

	// ndist is in km, skyPrecalcBoundary in km, earthAtmosphereRadius in km. scatteringdist is normalized.
	float scatteringdist = min(ndist,skyPrecalcBoundary * tileSizeKm)  / earthAtmosphereRadius;  // len  * 0.256
	//float scatteringdist = ndist / earthAtmosphereRadius;  // len  * 0.256

	//scatteringdist *= 2.0;

	vec3 eyeScatteringNorm = vec3(0.0,eyeHeightNorm,0.0);

	// if scatteringdist puts us outside the r=1 sphere, take it back
	//if (eyeHeightNorm + dir.y * scatteringdist > 1.0)	scatteringdist *= 0.5;

	float nearAirFactor = 10.0;

	// get total air mass between eye and end of scattering 
	float totalAir = pathAirMassFlat(eyeScatteringNorm,eyeScatteringNorm + dir * scatteringdist) * nearAirFactor;
	//float totalAir = pathAirMassSpherical(eyeScatteringNorm,eyeScatteringNorm + dir * scatteringdist) * nearAirFactor;

	vec3 absorbAmount = absorb(totalAir,vec3(1.0),scatterAbsorb);

	float fogAmount = 1.0 / (exp(totalAir));


	//c = vec3(1.0);

	// calculate absorbtion amount over airmass, apart from sky, which has this baked in
	//if (colourT.a < 1.0) c *= absorbAmount;
	//c *= absorbAmount;

	// add in scattering from sky dome, based on angle of view

	vec2 skyProbeDir = dir.xz;//(dir.y < 0.0) ? normalize(dir.xz) : dir.xz;

	vec3 skyProbe = texture(skylightSmoothTex,vec2(skyProbeDir * 0.48 + 0.5)).rgb;
	//c += skyProbe * totalAir;

	//skyProbe *= (vec3(1.0) + Kr * 0.8);

	//if (colourT.a>0.9999)fogAmount = 1.0;
	
	c = mix(skyProbe, c, fogAmount);


	//c = vec3(len / 1024.0);
	c += getSimpleScattering(eyeScatteringNorm, dir, sunVector, scatterAbsorb, scatteringdist, eyeShadow,nearAirFactor);
	//c += getRayMarchedScattering2(eye, dir, sunVector, scatterAbsorb, 0.0, min(distnorm, skyPrecalcBoundary)  / earthAtmosphereRadius );

	//c = ;

	//c += vec3(1.0,0.0,0.0) * eyeShadow;
	//c = vec3(totalAir,scatteringdist,0.0,(scatteringdist-totalAir) * 100.0) * 100.0;
	//c = vec3(max(0.0,scatteringdist-totalAir),max(0.0,totalAir-scatteringdist),0.0) * 1.0;
	//c = vec3(scatteringdist) * 100.0;
	//c = vec3(eyeHeightNorm);
	//c = vec3(totalAir);
	//c = vec3(fogAmount);
	
	out_Colour = vec4(c.rgb,1.0);
}
