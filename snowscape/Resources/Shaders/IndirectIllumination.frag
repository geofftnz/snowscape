#version 140
precision highp float;

uniform sampler2D heightTexture;
uniform sampler2D shadowTexture;
uniform sampler2D normalTexture;
uniform float texsize;
uniform vec3 sunVector;

in vec2 texcoord;

out float out_Indirect;

float texel = 1.0 / texsize;


float sampleHeight(vec2 pos)
{
	return textureLod(heightTexture,pos,0).r;
}

vec3 getNormal(vec2 pos)
{
    float h1 = sampleHeight(vec2(pos.x, pos.y - texel));
	float h2 = sampleHeight(vec2(pos.x, pos.y + texel));
    float h3 = sampleHeight(vec2(pos.x - texel, pos.y));
	float h4 = sampleHeight(vec2(pos.x + texel, pos.y));
	return normalize(vec3(h3-h4,2.0,h1-h2));
}

float getShadowForGroundPos(float h, float shadowHeight)
{
    return smoothstep(-1.0,-0.02,h - shadowHeight);
}

float getDirectIllumination(vec2 p, vec3 n, float h)
{
	return clamp(dot(n,sunVector),0.0,1.0) * getShadowForGroundPos(h,texture(shadowTexture,p).r);
}

float getSliceIndirect(vec2 p0, vec2 dp)
{
	// walk out across terrain
	// determine if sample is visible
	// determine if sample is lit
	// calculate diffuse-diffuse bounce  LDDE
	// calculate specular-diffuse bounce   LSDE
	
	float h0 = texture(heightTexture,p0).r; // height at origin
	float t = 0.0;
	float maxdydx = 0.0;
	float indirect = 0.0;
	
	for(t = 1.0; t < 50.0; t += 1.0 + t*0.25)
	{
		vec2 p = p0 + dp * t;
		float h = texture(heightTexture, p).r ;
		float dydx = (h - h0) / t;

		if (dydx > maxdydx){ // this sample is visible
			maxdydx = dydx;

			// get normal
			vec3 n = normalize(texture(normalTexture,p).rgb - vec3(0.5));

			float direct = getDirectIllumination(p,n,h);

			indirect += direct * 0.02;

		}

	}
		
	return indirect;
}


void main(void)
{

	float indirect = 0.0;

	for(float a = 0.0; a < 1.0; a += 1.0/19.0)
	{
		indirect += getSliceIndirect(texcoord,vec2(sin(a*6.2831854),cos(a*6.2831854))*texel);	
	}

	out_Indirect = indirect / 19.0;
}
