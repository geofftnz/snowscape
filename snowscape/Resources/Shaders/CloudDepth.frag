#version 140
precision highp float;

uniform sampler2D cloudTexture;
uniform vec3 sunDirection;
uniform vec3 cloudScale;

in vec2 cloudcoord;

out vec4 out_CloudDepth;

float cloudThickness(vec2 p)
{
	return max(texture2D(cloudTexture,p * cloudScale.xz).r - 0.3,0.0) * 1.4;  // * cloudScale.xz
}

float cloudDensity(vec3 p)
{
	float cloudMid = cloudScale.y * 0.5;
	float cloudThickness = cloudScale.y;
	float nt = cloudThickness(p.xz);

	float ctop = cloudMid + nt * cloudThickness;
	float cbottom = cloudMid - nt * cloudThickness;

	float d = clamp(max(min(ctop - p.y , p.y - cbottom),0.0),0.0,1.0);
	return d;
}

void main(void)
{
	//float c = texture2D(cloudTexture,cloudcoord).r;	
	//out_CloudDepth = vec4(c,cloudcoord.x,cloudcoord.y,1.0);

	// transform our texture coords into world coords
	vec2 ccworld = cloudcoord.xy / cloudScale.xz;

	// start at bottom of cloud layer (will need to change this depending on sign of sunDirection.y
	vec3 p0 = vec3(ccworld.x,0.0f,ccworld.y);

	vec3 dir = sunDirection;

	// calculate intersection of top cloud layer plane, in world coordinates
	float maxt = cloudScale.y / dir.y;

	float cloudStart=1.0,cloudEnd=0.0;
	float totalDensity=0.0;
	float dt = 1.0 / 1024.0;

	for(float t = 0.0; t <= 1.0; t += dt)
	{
		vec3 p = p0 + sunDirection * t * maxt;

		float d = cloudDensity(p);
		//float d = cloudThickness(p.xz);
		if (d > 0.5)
		{
			cloudStart = min(t,cloudStart);
			cloudEnd = t;
			totalDensity += dt * d;
		}
	}

	out_CloudDepth = vec4(cloudStart,cloudEnd,totalDensity,1.0);
}
