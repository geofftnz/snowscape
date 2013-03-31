#version 140
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
	vec2 d = normalize(sunDirection.xz) * texel;  // amount to move each step
	float dh = sunDirection.y;  // change in height on each step
	float h0 = texture2D(heightTexture,p).r; // current height
	//float h = h0;

	vec3 pp = vec3(p,h0);
	vec3 dpp = vec3(d,dh);

	// sun below horizon - shadow is very high
	if (dh <= 0.0)
	{
		return 65000.0;
	}

	// sun straight overhead, shadow on/under ground
	if (dh > 0.999999)
	{
		return h0;
	}

	float maxH = -1.0; // current max height of shadow-plane

	pp += dpp;
	while (pp.z < maxHeight)
	{
		maxH = max(maxH, texture2D(heightTexture,pp.xy).r - pp.z);
		pp += dpp;
	}

	return maxH + h0;
}


void main(void)
{
	out_ShadowAO.r = getShadowHeight(terraincoord);
	out_ShadowAO.g = 0.0;
}
