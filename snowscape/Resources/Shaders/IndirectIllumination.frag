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


mat3 m = mat3( 0.00,  0.80,  0.60,
              -0.80,  0.36, -0.48,
              -0.60, -0.48,  0.64 );

float rand(vec2 co){
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

float rand(vec3 co){
    return fract(sin(dot(co.xyz ,vec3(12.9898,78.233,47.985))) * 43758.5453);
}

// credit: iq/rgba
float hash( float n )
{
    return fract(sin(n)*43758.5453);
}


// credit: iq/rgba
float noise( in vec3 x )
{
    vec3 p = floor(x);
    vec3 f = fract(x);
    f = f*f*(3.0-2.0*f);
    float n = p.x + p.y*57.0 + 113.0*p.z;
    float res = mix(mix(mix( hash(n+  0.0), hash(n+  1.0),f.x),
                        mix( hash(n+ 57.0), hash(n+ 58.0),f.x),f.y),
                    mix(mix( hash(n+113.0), hash(n+114.0),f.x),
                        mix( hash(n+170.0), hash(n+171.0),f.x),f.y),f.z);
    return res;
}


// credit: iq/rgba
float fbm( vec3 p )
{
    float f;
    f  = 0.5000*noise( p );
    p = m*p*2.02;
    f += 0.2500*noise( p );
    p = m*p*2.03;
    f += 0.1250*noise( p );
    p = m*p*2.01;
    f += 0.0625*noise( p );
    return f;
}



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
	float maxdydx = -1000.0;
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

			float diffuse = direct * (0.8 + 0.2 * n0dotl) * falloff;

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
float patchVisibility(vec2 pp0, vec2 pp1, float h0, vec3 n0)
{
	float h1 = texture(heightTexture,pp1).r;

	float s = getShadowForGroundPos(h1,texture(shadowTexture,pp1).r);
	if (s <= 0.0){
		return 0.0;
	}

	vec3 p0 = vec3(pp0.x * texsize,h0,pp0.y * texsize);
	vec3 p1 = vec3(pp1.x * texsize,h1,pp1.y * texsize);


	float l = length(p1.xz-p0.xz);
	
	if (l<1.0){
		return 0.0;
	}

	vec3 dp = (p1-p0)/l;

	for (float t = 0.0; t <= l; t+= 1.0)
	{
		vec3 p = p0 + dp * t;
		float h = texture(heightTexture,p.xz * texel).r;
		if (h > p.y){
			return 0.0;
		}
	}

	// patch visible, calculate lighting

	
	vec3 n1 = normalize(texture(normalTexture,pp1).rgb - vec3(0.5));  // normal at target

	vec3 dpn = normalize(dp);
	float vf = clamp(dot(n0,dpn),0.0,1.0) * clamp(dot(n1,-dpn),0.0,1.0);

	// direct illumination
	float direct = clamp(dot(n1,sunVector),0.0,1.0) * vf;

	//return direct / (1.0 + l * l * 0.1);
	l *= 0.1;
	return direct / (3.1415927 * l * l);
}

float indirectForPatch(vec2 pp0, vec2 pp1, float h0, vec3 n0)
{
	return patchVisibility(pp0,pp1,h0,n0);
}

float getIndirectRect(vec2 pp0)
{
	float indirect = 0.0;

	float h0 = texture(heightTexture,pp0).r;
	vec3 n0 = normalize(texture(normalTexture,pp0).rgb - vec3(0.5));  // normal at origin

	for (float y = -5.0; y<= 5.0; y+= 1.0){
		for (float x = -5.0; x<= 5.0; x+= 1.0){
			indirect += indirectForPatch(pp0,pp0 + vec2(x,y) * texel,h0,n0);
		}
	}

	return indirect;
}

float getIndirectRandom(vec2 pp0)
{
	float h0 = texture(heightTexture,pp0).r;
	vec3 n0 = normalize(texture(normalTexture,pp0).rgb - vec3(0.5));  // normal at origin
	float indirect = 0.0;

	for (float i=0.0;i<10.0;i++)
	{

		float a = hash(hash(i + pp0.x)) * 3.1415927 * 2.0;
		float r = hash(hash(i + pp0.y + 173.6)) * 50.0;

		vec2 pp1 = pp0 + vec2(cos(a),sin(a)) * r * texel;

		indirect += indirectForPatch(pp0,pp1,h0,n0);
	}

	return indirect;
}

float getIndirectRadial(vec2 pp0)
{
	float h0 = texture(heightTexture,pp0).r;
	vec3 n0 = normalize(texture(normalTexture,pp0).rgb - vec3(0.5));  // normal at origin
	float indirect = 0.0;

	for (float a = 0.0; a< 1.0; a+= 0.1)
	{
		for(float r = 1.0; r< 20.0; r *= 1.2)
		{
			vec2 pp1 = pp0 + vec2(cos(a*6.282),sin(a*6.282)) * r * texel;

			indirect += indirectForPatch(pp0,pp1,h0,n0);
		}
	}

	return indirect;
}

void main(void)
{

	//out_Indirect = getIndirectRect(texcoord);
	//out_Indirect = getIndirectRandom(texcoord);
	out_Indirect = getIndirectRadial(texcoord) * 0.2;
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
