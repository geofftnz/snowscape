#version 140
precision highp float;

uniform sampler2D colTex;
uniform sampler2D histogramTex;
uniform float exposure;
uniform float whitelevel;
uniform float blacklevel;

uniform float time;

// FXAA
uniform vec2 fxaaQualityRcpFrame;
uniform float fxaaQualitySubpix;
uniform float fxaaQualityEdgeThreshold;
uniform float fxaaQualityEdgeThresholdMin;


noperspective in vec2 texcoord0;
out vec4 out_Colour;

#include "noise.glsl"

const vec3 luminance = vec3(0.2126,0.7152,0.0722);

vec4 getSample(vec2 p)
{
	vec3 col = textureLod(colTex,p,0).rgb;

	// set black level
	//col.rgb -= vec3(blacklevel);
	col.rgb = mix(pow(col.rgb,vec3(1.0 + blacklevel)),col.rgb,min(1.0,pow(dot(col,luminance),3.0)));
	
	
	// apply exposure
	//col.rgb = vec3(1.0) - exp(col.rgb * exposure);
	col.rgb *= -exposure;

	// reinhard tone map
	col.rgb = (col.rgb  * (vec3(1.0) + (col.rgb / (whitelevel * whitelevel))  ) ) / (vec3(1.0) + col.rgb);

	// gamma correction
	col = pow(col.rgb,vec3(1.0/2.2));
	
	vec4 lcol = vec4(col, dot(col,luminance));
	return lcol;
}

#include "fxaa.glsl"


void main(void)
{
	vec4 col;
	col = getSample(texcoord0); 

	// if (texcoord0.x < 0.5)
	// {
		// col = getSample(texcoord0); 
	// }
	// else
	// {
		// col = FxaaPixelShader(texcoord0);
	// }
	

	//float n = hash(time + hash(texcoord0.x) + hash(texcoord0.y));
	//col *= (1.0 - n * 0.1);

	// output
	out_Colour = col;
}


/*
float A = 0.15;
float B = 0.50;
float C = 0.10;
float D = 0.20;
float E = 0.02;
float F = 0.30;
float W = 11.2;

vec3 Uncharted2Tonemap2(vec3 x)
{
   return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
}

vec3 Uncharted2Tonemap(vec3 col)
{
	vec3 colA = col * A;
	return (
				(col * (colA + vec3(C*B)) + vec3(D*E)) / 
				(col * (colA + vec3(B)) + vec3(D*F))
		   ) - vec3(E/F);
}
*/