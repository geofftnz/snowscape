﻿#version 140
precision highp float;

uniform sampler2D heightTexture;
uniform vec3 sunDirection;
uniform float minHeight;
uniform float maxHeight;

in vec2 terraincoord;

out vec2 out_ShadowAO;

float texel = 1.0 / 1024.0;

// gets the height (in world coordinates) of the shadow-plane above this sample.
float getShadowHeight(vec2 p)
{
	float h0 = texture2D(heightTexture,p).r; // current height
	vec2 d = normalize(sunDirection.xz) * texel;  // amount to move each step
	float l = length(sunDirection.xz);

	// sun straight overhead, shadow on/under ground
	if (l<=0.000001)
	{
		return h0;
	}

	float dh = sunDirection.y / l;  // change in height on each step
	//float h = h0;

	vec3 pp = vec3(p,h0); 
	vec3 dpp = vec3(d,dh);

	// sun below horizon - shadow is very high
	if (dh <= 0.0)
	{
		return 65000.0;
	}

	float maxH = -1.0; // current max height of shadow-plane
	float i = 0.0f;

	//pp += dpp;
	while (pp.z < maxHeight && i < 2048.0f)
	{
		maxH = max(maxH, texture(heightTexture,pp.xy).r - pp.z);
		pp += dpp;
		i += 1.0f;
	}

	return maxH + h0;
}


float getSliceVisibility(vec2 p, vec2 dp)
{
	float h0 = texture(heightTexture,p).r; // height at origin
	float t = 0.0;
	float dydx = 0.0;
	
	for(t = 1.0; t < 50.0; t += 1.0 + t*0.25)
	{
		dydx = max(dydx, (texture(heightTexture, p + dp * t ).r - h0) / t);
	}

	return 1.0 - atan(dydx) / 1.57079635;
}


// returns the proportion of sky visible over the up-facing hemisphere centred on this point
float getSkyVisibility(vec2 p)
{
	float vis = 0.0;

	for(float a = 0.0; a < 1.0; a += 1.0/19.0)
	{
		vis += getSliceVisibility(p,vec2(sin(a*6.2831854),cos(a*6.2831854))*texel);	
	}

	return vis / 19.0;
}


void main(void)
{
	out_ShadowAO.r = getShadowHeight(terraincoord);
	out_ShadowAO.g = getSkyVisibility(terraincoord);
}
