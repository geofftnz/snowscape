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
	vec3 n0 = normalize(texture(normalTexture,p0).rgb - vec3(0.5));  // normal at origin
	float t = 0.0;
	float maxdydx = 0.0;
	float indirect = 0.0;

	//vec3 avgdir = vec3(dp.x,0.0,dp.y);
	
	for(t = 0.25; t < 200.0; t += t * 1.05)
	{
		vec2 p = p0 + dp * t;
		float h = texture(heightTexture, p).r ;
		float dydx = (h - h0) / t;

		if (dydx <= maxdydx){ // this sample is visible
			

			// get normal
			vec3 n = normalize(texture(normalTexture,p).rgb - vec3(0.5));

			//float direct = getDirectIllumination(p,n,h);

			// get view ray direction from p0 to p
			vec3 lray = vec3(p.x * texsize,h,p.y  * texsize) - vec3(p0.x  * texsize,h0,p0.y  * texsize);

			// TODO: we should be using BRDF of p with i=sunVector, o=lray

						// work out diffuse falloff as 1/r^2
			float falloff = 1.0 / (1.0 + dot(lray,lray)*0.1);//(texel / (dot(lray,lray) * 0.1));

			vec3 lrayd = normalize(lray);

			float s = getShadowForGroundPos(h,texture(shadowTexture,p).r);

			float n0dotl = clamp(dot(n0,lrayd),0.0,1.0);

			// get direct illumination at point p
			float direct = clamp(dot(n,sunVector),0.0,1.0) * s;

			float diffuse = direct * n0dotl * falloff;

			// light specularly reflected from L off P towards P0
			//vec3 rsun = reflect(sunVector, n);
			//float specular = pow(clamp(dot(lrayd, -rsun),0.0,1.0),20.0) * s * n0dotl * falloff;
			float specular = 0.0;

			indirect += diffuse + specular;

			//avgdir += lrayd * (diffuse + specular);

		}
		maxdydx = max(dydx,maxdydx);

	}
		
	//return vec4(normalize(avgdir),indirect * 0.5);
	return indirect;
}

// return 1 if patch p1 is visible from patch p0
float patchVisibility(vec2 pp0, vec2 pp1)
{
	float h0 = texture(heightTexture,pp0).r;
	float h1 = texture(heightTexture,pp1).r;

	float s = getShadowForGroundPos(h1,texture(shadowTexture,pp1).r);
	if (s <= 0.0){
		return 0.0;
	}

	vec3 p0 = vec3(pp0.x * texsize,h0,pp0.y * texsize);
	vec3 p1 = vec3(pp1.x * texsize,h1,pp1.y * texsize);

	vec3 n0 = normalize(texture(normalTexture,pp0).rgb - vec3(0.5));  // normal at origin
	vec3 n1 = normalize(texture(normalTexture,pp1).rgb - vec3(0.5));  // normal at target

	float l = length(p1.xz-p0.xz);
	
	if (l<1.0){
		return 0.0;
	}

	vec3 dp = (p1-p0)/l;
	vec3 dpn = normalize(dp);

	float vf = dot(n0,dpn) * dot(n1,-dpn);

	for (float t = 0.0; t <= l; t+= 1.0)
	{
		vec3 p = p0 + dp * t;
		float h = texture(heightTexture,p.xz * texel).r;
		if (h > p.y){
			return 0.0;
		}
	}

	// direct illumination
	float direct = clamp(dot(n1,sunVector),0.0,1.0) * vf;

	return direct / (3.1415927 * l * l);
}

float indirectForPatch(vec2 pp0, vec2 pp1)
{
	return patchVisibility(pp0,pp1);
}

float getIndirectRect(vec2 pp0)
{
	float indirect = 0.0;

	for (float y = -5.0; y<= 5.0; y+= 1.0){
		for (float x = -5.0; x<= 5.0; x+= 1.0){
			indirect += indirectForPatch(pp0,pp0 + vec2(x,y) * texel);
		}
	}

	return indirect;
}



void main(void)
{

	out_Indirect = getIndirectRect(texcoord);
//
	//float indirect = 0.0;
	////vec3 avgdir = vec3(0.0);
//
	//for(float a = 0.0; a < 1.0; a += 1.0/39.0)
	//{
		//float ind = getSliceIndirect(texcoord,vec2(sin(a*6.2831854),cos(a*6.2831854))*texel);	
		//indirect += ind;
		////avgdir += ind.xyz * ind.a;
	//}
//
	////out_Indirect = vec4(normalize(avgdir) * 0.5 + vec3(0.5), indirect / 39.0);
	//out_Indirect = indirect / 39.0;
}
