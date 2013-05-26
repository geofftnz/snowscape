#version 140
precision highp float;

uniform sampler2D cloudTexture;
uniform vec3 sunDirection;
uniform vec3 cloudScale;

in vec2 cloudcoord;

out vec4 out_CloudDepth;

float cloudThickness(vec2 p)
{
	return max(texture(cloudTexture,p * cloudScale.xz).r - 0.3,0.0) / 0.7;  // * cloudScale.xz
}

float cloudThicknessNoScale(vec2 p)
{
	return max(texture(cloudTexture,p).r - 0.3,0.0) / 0.7;  // * cloudScale.xz
}

float cloudDensity(vec3 p)
{
	float cloudMid = cloudScale.y * 0.5;
	float cloudThickness = cloudScale.y;
	float nt = cloudThickness(p.xz) * 0.5;

	float ctop = cloudMid + nt * cloudThickness;
	float cbottom = cloudMid - nt * cloudThickness;

	float d = clamp(max(min(ctop - p.y , p.y - cbottom),0.0),0.0,1.0);
	return d;
}

float texel = 1.0 / 1024.0;

float cloudShadow(vec2 p)
{

	return cloudThicknessNoScale(p);

	float cloudHalfThickness = cloudScale.y * 0.5;
	vec2 d = normalize(sunDirection.xz) * (vec2(1.0) / cloudScale.xz);  // amount to move each step
	float dh = sunDirection.y;  // change in height on each step
	float h0 = cloudThickness(p) * cloudHalfThickness; // current height}

	vec3 pp = vec3(p.x,h0,p.y);
	vec3 dpp = vec3(d.x,dh,d.y);
	float light = 1.0;
	float i = 0.0f;

	// starting at current position, move towards sun until we hit something or the top of the cloud layer
	while (pp.z < cloudHalfThickness && i < 2048.0)
	{
		pp += dpp;
		i += 1.0f;

		float c = cloudThickness(pp.xz) * cloudHalfThickness * 0.5;
		if (c > pp.y)
		{
			light = 0.0;
			break;
		}
	}
	return light;
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
		if (d > 0.0)
		{
			cloudStart = min(t,cloudStart);
			cloudEnd = t;
			totalDensity += dt;
		}
	}

	float topLight = cloudShadow(cloudcoord.xy);

	out_CloudDepth = vec4(cloudStart,cloudEnd,totalDensity,topLight);
}
