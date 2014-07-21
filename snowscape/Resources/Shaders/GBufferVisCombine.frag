﻿#version 140
precision highp float;
uniform sampler2D normalTex;
uniform sampler2D normalLargeScaleTex;
uniform sampler2D paramTex;
uniform sampler2D heightTex;
uniform sampler2D shadeTex;
uniform sampler2D indirectTex;
uniform sampler2D depthTex;

uniform sampler2D miscTex;
uniform sampler2D miscTex2;

uniform samplerCube skyCubeTex;
uniform vec4 boxparam;
uniform vec3 eyePos;
uniform vec3 sunVector;
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


uniform mat4 pre_projection_matrix;

in vec2 texcoord0;
out vec4 out_Colour;

vec3 horizonLight(vec3 eye, vec3 dir, float groundheight, float factor);
float cloudSunAbsorb(vec3 p);
// air absorbtion
//vec3 Kr = vec3(0.18867780436772762, 0.4978442963618773, 0.6616065586417131);



vec3 Kr2 = Kr;
//vec3 Kr2 = vec3(0.100,0.598,0.662) * 1.4; // just making shit up
//vec3 Kr2 = vec3(2.284, 3.897, 8.227) * 0.11;

// raleigh scattering constants - maybe
//vec3 Kral = vec3(2.284, 3.897, 8.227) * 0.2;
vec3 Kral = Kr;
// inverse eye response - for mapping atmospheric scattering amounts to something more realistic
//vec3 Er = vec3(0.6,0.6,1.0);
mat3 m = mat3( 0.00,  0.80,  0.60,
              -0.80,  0.36, -0.48,
              -0.60, -0.48,  0.64 );
//vec3 sunLight = vec3(8.0);
float intersectBox ( vec3 rayo, vec3 rayd, vec3 boxMin, vec3 boxMax)
{
    vec3 omin = ( boxMin - rayo ) / rayd;
    vec3 omax = ( boxMax - rayo ) / rayd;
    vec3 tmax = max ( omax, omin );
    vec3 tmin = min ( omax, omin );
    float t1 = min ( tmax.x, min ( tmax.y, tmax.z ) );
    float t2 = max ( max ( tmin.x, 0.0 ), max ( tmin.y, tmin.z ) );
    return min(t1,t2);
}


float intersectBoxInside  ( vec3 rayo, vec3 rayd, vec3 boxMin, vec3 boxMax)
{
    vec3 omin = ( boxMin - rayo ) / rayd;
    vec3 omax = ( boxMax - rayo ) / rayd;
    vec3 tmax = max ( omax, omin );
    return min ( tmax.x, min ( tmax.y, tmax.z ) );
}


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


float texel = 1.0 / boxparam.x;
float sampleHeight(vec2 posTile)
{
    return texture(heightTex,posTile * texel).r;
}

// pos in tile coords (0-boxparam.xy)
vec3 getNormal(vec2 pos)
{
	float t = 1.0;
    float h1 = sampleHeight(vec2(pos.x, pos.y - t));
    float h2 = sampleHeight(vec2(pos.x, pos.y + t));
    float h3 = sampleHeight(vec2(pos.x - t, pos.y));
    float h4 = sampleHeight(vec2(pos.x + t, pos.y));
    return normalize(vec3(h3-h4,t* 2.0,h1-h2));
}

/*
float sampleHeightNoise(vec2 posTile, float f, float a)
{
    return sampleHeight(posTile) + fbm(vec3(posTile.xy*f,0.0)) * a;
}vec3 getNormalNoise(vec2 pos, float f, float a)
{
    float h1 = sampleHeightNoise(vec2(pos.x, pos.y - 0.5),f,a);
    float h2 = sampleHeightNoise(vec2(pos.x, pos.y + 0.5),f,a);
    float h3 = sampleHeightNoise(vec2(pos.x - 0.5, pos.y),f,a);
    float h4 = sampleHeightNoise(vec2(pos.x + 0.5, pos.y),f,a);
    return normalize(vec3(h3-h4,1.0,h1-h2));
}*/



// mie phase - @pyalot http://codeflow.org/entries/2011/apr/13/advanced-webgl-part-2-sky-rendering/
float phase(float alpha, float g){
    float gg = g*g;
    float a = 3.0*(1.0-gg);
    float b = 2.0*(2.0+gg);
    float c = 1.0+alpha*alpha;
    float d = pow(1.0+gg-2.0*g*alpha, 1.5);
    return (a/b)*(c/d);
}


float adepthTerrain(vec3 eye, vec3 target)
{
    return length(target - eye);
}


// atmospheric depth for sky ray - @pyalot http://codeflow.org/entries/2011/apr/13/advanced-webgl-part-2-sky-rendering/
// returns in the range 0..1 for eye inside atmosphere
float adepthSky(vec3 eye, vec3 dir)
{
    float a = dot(dir, dir);
    float b = 2.0*dot(dir, eye);
    float c = dot(eye, eye)-1.0;
    float det = b*b-4.0*a*c;
    float detSqrt = sqrt(det);
    float q = (-b - detSqrt)/2.0;
    float t1 = c/q;
    return t1;
}



float adepthGround(vec3 eye, vec3 dir, float groundheight)
{
    float a = dot(dir, dir);
    float b = 2.0*dot(dir, eye);
    float c = dot(eye, eye) - groundheight*groundheight;
    float det = b*b-4.0*a*c;
    if (det<0.0) return 1000.0;
    float detSqrt = sqrt(det);
    a+=a;
    float t1 = (-b - detSqrt) / a;
    float t2 = (-b + detSqrt) / a;
    float tmin = min(t1,t2);
    float tmax = max(t1,t2);
    if (tmin >= 0.0) // both in front of viewer
	{
        return tmin;
    }


	if (tmax >= 0.0) // one in front, one behind
	{
        return tmax;
    }


	return 1000.0;
}


float adepthSkyGround(vec3 eye, vec3 dir, float groundheight)
{
    return min(adepthSky(eye,dir),adepthGround(eye,dir,groundheight));
}



// exponential absorbtion - @pyalot http://codeflow.org/entries/2011/apr/13/advanced-webgl-part-2-sky-rendering/
vec3 absorb(float dist, vec3 col, float f)
{
    return col - col * pow(Kr2, vec3(f / dist));
    //vec3 k = pow(Kr, vec3(f / dist));
	//return col - col * k;
	//return col - col * k;
}

vec3 absorb(float dist, vec3 col, vec3 K, float f)
{
    return col - col * pow(K, vec3(f / dist));
    //vec3 k = pow(Kr, vec3(f / dist));
	//return col - col * k;
	//return col - col * k;
}

// hopefully the inverse of absorb
vec3 inscatter(float dist, vec3 col, float f)
{
    return col * pow(Kr2, vec3(f / dist));
}

//float absorbflat(float dist, float x, float K, float f)
//{
	//return x - x * pow(K, f / dist);
//}
//
vec3 getSkyColour(vec3 skyvector)
{
	return textureLod(skyCubeTex,skyvector,0).rgb;
}


float getShadowForGroundPos(vec3 p, float shadowHeight)
{
    return smoothstep(-1.0,-0.02,p.y - shadowHeight);// * getCloudShadow(p);
}


float getShadow(vec3 p)
{
	return step(0.0,p.y - texture(shadeTex,p.xz * texel).r);// * getCloudShadow(p);
}


// this reduces the contribution of skylight scatter in areas close to the ground that see less sky.
// this is so subtle it might as well get left out
float getAOInfluence(vec3 p)
{
	float influence = clamp((p.y - sampleHeight(p.xz)) / aoInfluenceHeight,0.0,1.0);
	float ao = texture(shadeTex,p.xz * texel).g;
	ao *= ao;
	return mix(ao,1.0,influence);
}


float directIllumination(vec3 p, vec3 n, float shadowHeight)
{
    return  getShadowForGroundPos(p, shadowHeight) * clamp(dot(n,sunVector),0,1);
}


vec3 eyeToSkyCoords(vec3 eye)
{
    float rearth = 6371000.0;
    float hsky = 50000;
    float hterrainunit = 4;
    float terraintosky = hterrainunit / (rearth + hsky);
    float groundbase = rearth / (rearth + hsky);
    return eye * terraintosky + vec3(0.0,groundbase,0.0);
    // eye position scaled so that radius of earth + atmosphere = 1
}


// this is assumed to be constant across the entire terrain, because the terrian is small compared to the atmosphere
// this should be moved to a uniform
vec3 sunIntensity()
{
    //vec3 influx = absorb(sample_depth, sunIntensity, scatteramount) * horizonLight(psun,sunVector,groundLevel,scatteramount);
	//return absorb(adepthSky(vec3(0.0,0.9,0.0), sunVector), sunLight, scatterAbsorb); // 28.0
	vec3 p = vec3(0.0,groundLevel * 1.001,0.0);
    return absorb(adepthSky(p, sunVector), sunLight, scatterAbsorb) * horizonLight(p,sunVector,groundLevel,scatterAbsorb);
}

vec3 gc(float r, float g, float b)
{
	return pow(vec3(r,g,b) / vec3(255.0),vec3(2.2));
}


vec3 terrainDiffuse(vec3 p, vec3 n, vec4 s, float shadowHeight)
{
    vec3 colH1 = gc(100,105,110);//  pow(vec3(182,180,196) / vec3(255.0),vec3(2.2));
    vec3 colL1 = gc(30,64,5); //pow(vec3(158,136,79) / vec3(255.0),vec3(2.2));
    //vec3 colH1 = pow(vec3(0.3,0.247,0.223),vec3(2.0));
    //vec3 colL1 = pow(vec3(0.41,0.39,0.16),vec3(2.0));
    //vec3 colW = gc(150,150,200); //pow(vec3(0.7,0.8,1.0),vec3(2.0));
    float looseblend = s.r*s.r;
    vec3 col = mix(colH1,colL1,looseblend);
    vec3 eyeDir = normalize(p-eyePos);

    //vec3 wCol = vec3(0.1,0.2,0.25) + getSkyColour(reflect(eyeDir,n)) * smoothstep(-2.0,-0.1,p.y - shadowHeight);
	vec3 colW = getSkyColour(n) + getSkyColour(reflect(eyeDir,n)) * getShadowForGroundPos(p,shadowHeight);

    //vec3 colW0 = wCol;
    // blue water
	//vec4 colW0 = vec4(0.4,0.7,0.95,1.0);  // blue water
	//vec3 colW1 = vec3(0.659,0.533,0.373);
    // dirty water
	//vec3 colW2 = vec3(1.2,1.3,1.4);
    // white water

	//colW = mix(colW0,colW1,clamp(s.b*1.5,0,1));
    // make water dirty->clean

	float waterblend = smoothstep(0.02,0.1,s.g) * 0.1 + 0.4 * s.g * s.g;
    col = mix(col,colW,waterblend);
    // water

    // misc vis
	vec3 colE = vec3(0.4,0.6,0.9);
    col += colE * clamp(s.a,0.0,1.0);
    return col;
}

vec3 terrainDiffuseDebug(vec3 p, vec3 n, vec4 s, float shadowHeight)
{
	vec3 col = vec3(0.1,0.06,0.05);

	float soft = s.r;
	vec3 softcol = vec3(0.4,0.8,0.3);
	col = mix(col, softcol, clamp(soft * 16.0,0.0,1.0));

	// water depth
	float waterdepth = s.g*s.g;
	vec3 watercol = mix(vec3(0.001,0.4,0.6),vec3(0.005,0.02,0.25),clamp(s.g*16.0,0.0,1.0));
	col = mix(col, watercol, smoothstep(0.0001,0.001,waterdepth));

	float suspended = s.b * 0.2;
	vec3 suspendedcol = vec3(0.6,0.0,0.05);
	col = mix(col, suspendedcol,clamp(suspended,0.0,1.0));

    return col;
}

vec3 terrainDiffuseDebugSnow(vec3 p, vec3 n, vec4 s, float shadowHeight)
{
	vec3 col = vec3(0.1,0.06,0.05);

	float slope = 1.0-clamp(dot(n,vec3(0.0,1.0,0.0)),0.0,1.0);
	float snowadjust = (slope*slope) * snowSlopeDepthAdjust;

	float snow = max(0.0,s.r + s.g - snowadjust);
	vec3 snowcol = vec3(0.98);
	//col = mix(col, snowcol, clamp(snow * 16.0,0.0,1.0));
	col = mix(col, snowcol, smoothstep(0.0,0.1,snow));

    return col;
}



vec3 getSkyLightFromDirection(vec3 dir, vec3 base)
{
	vec3 col = textureLod(skyCubeTex,base,9).rgb;
	//return col * (dot(dir, base) * 0.5 + 0.5);
	return col * clamp(dot(dir, base),0.0,1.0);
}

vec3 getSkyLight(vec3 dir)
{
	vec3 col = vec3(0.0);

	col += getSkyLightFromDirection(dir, vec3(0.0,1.0,0.0));  // straight up
	col += getSkyLightFromDirection(dir, vec3(1.0,0.2,0.0));  // compass direction
	col += getSkyLightFromDirection(dir, vec3(-1.0,0.2,0.0));  // compass direction
	col += getSkyLightFromDirection(dir, vec3(0.0,0.2,1.0));  // compass direction
	col += getSkyLightFromDirection(dir, vec3(0.0,0.2,-1.0));  // compass direction

	return col * 0.2;
}


// this is screwed.
vec3 brdfSunSnow(vec3 p, vec3 n, vec3 l, vec3 e, vec3 influx)
{
	// diffuse
	float dif = clamp(dot(n,l),0.0,1.0);

	// specular facets
    vec3 refl = reflect(e,n);
    vec3 spec = influx * pow(clamp(dot(l,refl),0.0,1.0),2.0) * 0.25;

    vec3 totalspec = vec3(0.0);
    vec3 totaldif = vec3(0.0);

    for(float i=0.0;i<10.0;i+=1.0){
        vec3 p2 = (p)*3.0 + vec3(i*19.3);
        vec3 n2 = normalize((vec3(noise(p2),noise(p2*1.5),noise(p2*7.1))-vec3(0.5)));
        float facetintensity = 0.1 + rand(p2);

        //float fre = pow( clamp(1.0+dot(nor2,rd),0.0,1.0), 2.0 );

        vec3 refl2 = reflect(e,n2);
        totaldif += getSkyLight(refl2);
        totalspec += pow(clamp(dot(l,refl2),0.0,1.0),200.0) * (clamp(dot(n,l)+0.1,0.0,1.0)) * facetintensity;// * fre;
    }

	return influx * vec3(0.9) * dif + totaldif * 0.01 + totalspec;
}


vec3 brdfRock(vec3 pos, vec3 norm, vec3 light, vec3 view, vec3 influx)
{
//vec3(0.3216,0.2078,0.1686)
//vec3(0.2549,0.1490,0.0863)
//vec3(0.3020,0.2000,0.1137)
//vec3(0.2941,0.2000,0.1137)
//vec3(0.2941,0.1529,0.0980)
//vec3(0.3569,0.2078,0.1255)
//vec3(0.4196,0.2824,0.2275)
//vec3(0.6549,0.5804,0.4667)
//vec3(0.4902,0.3961,0.2784)

	// rock is primarily diffuse, with largish specular highlight
	return vec3(0.0);
}



vec3 generateCol(vec3 p, vec3 nd, vec3 nls, vec4 s, vec3 eye, float shadowHeight, float AO)
{

	//return n.xyz * 0.5 + vec3(0.5);
	//float normalblend = smoothstep(normalBlendNearDistance,normalBlendFarDistance, length(p - eye));
	float normalblend = clamp((length(p - eye) - normalBlendNearDistance) / (normalBlendFarDistance-normalBlendNearDistance),0.0,1.0);
	vec3 n = normalize(mix(nd,nls,normalblend));


    //vec3 col = terrainDiffuse(p,n,s,shadowHeight);
	vec3 col = vec3(pow(0.98,2.2));

	if (abs(renderMode-1.0) < 0.1){
		col = terrainDiffuseDebug(p,n,s,shadowHeight);
		//col = terrainDiffuse(p,n,s,shadowHeight);
	}
	if (abs(renderMode-2.0) < 0.1){
		col = terrainDiffuseDebugSnow(p,n,s,shadowHeight);
	}

    //float diffuse = directIllumination(p,n,shadowHeight);
	//col = col * diffuse + col * vec3(0.8,0.9,1.0) * 0.7 * AO;
	//min(getShadow(p),cloudSunAbsorb(p))

	vec3 light = vec3(0.0);
	vec3 sunLight = sunIntensity();


	
	// direct illumination from sun
	light += sunLight * clamp(dot(n,sunVector),0.0,1.0) * getShadowForGroundPos(p,shadowHeight);
	//light += brdfSunSnow(p,n,sunVector,normalize(p-eye),sunLight * getShadowForGroundPos(p,shadowHeight));

	// glow through snow at grazing angles
	//light += sunLight * vec3(0.2,0.25,0.4) * clamp(dot(n,sunVector)+0.1,0.0,1.0) * getShadowForGroundPos(p,shadowHeight);
	//float nds = dot(n,sunVector);
	//float sg = pow(smoothstep(-0.3,0.0,nds) * (1.0 - smoothstep(0.1,0.3,nds)),2.0) * 0.1;
	//light += sunLight * Kr * sg * getShadowForGroundPos(p,shadowHeight);
	//pow(Kr,vec3(1.0 + 10.0 * clamp(-nds,0.0,1.0)))

	// indirect illumination from terrain-bounce
	//vec4 ind = texture(indirectTex,p.xz * texel);
	//vec3 indd = normalize(ind.xyz - vec3(0.5));
	float ind = texture(indirectTex,p.xz * texel).r;

	//light += sunLight * clamp(dot(n,indd),0.0,1.0) * ind.a;
	vec3 indn = normalize(vec3(nls.x,-0.2,nls.z));

	light += sunLight * vec3(0.7,0.85,1.0) * vec3(ind) * indirectBias * (clamp(dot(n,indn),0.0,1.0));  // vertical slopes would be fully lit.

	// indirect illumination from sky-dome
	//light += getSkyLight(n) * AO;
	//light += mix(getSkyLight(n),sunLight,ambientBias) * AO;
	//light += textureLod(skyCubeTex,n,0).rgb * 10.0 * ambientBias * AO;

	light += getSkyLight(n) * 10.0 * ambientBias * AO;// * (clamp(n.y,0.0,1.0) * 0.5 + 0.5);

	vec3 col2 = col * light;

    return col2;
	// can probably ignore the aborption between point and eye
	//return absorb(adepthTerrain(eye, p) * 0.0001,col2,scatterAbsorb);
	//return absorb(adepthTerrain(eye, p) * sampleDistanceFactor,col2,scatterAbsorb);
}



// fake refracted light
vec3 horizonLight(vec3 eye, vec3 dir, float groundheight, float factor)
{
    //vec3 sd=sph[i].xyz-p;    
    //b = dot ( rd,sd );
    //disc = b*b + sph[i].w - dot ( sd,sd );
    //if (disc>0.0) tnow = b - sqrt(disc);
//

	// intersect the eye-->dir ray with the radius=groundheight sphere centred on 0,0,0
	float a = dot(dir, dir);
    float b = 2.0 * dot(dir, eye);
    float c = dot(eye, eye) - groundheight*groundheight;
    float det = b*b - 4.0*a*c;
    //float b = dot(dir, eye);
	//float det = b*b - dot(eye,eye) + groundheight*groundheight;

	// no intersection
	if (det < 0.0)
	{
        return vec3(1.0);
    }


	// calculate intersections. If both are negative, then the ray escapes through space to the sun
	// if one is positive and one is negative, we're under ground
	// otherwise we're interested in the chord length
	det = sqrt(det);
    a += a;
    float t1 = (-b - det) / a;
    float t2 = (-b + det) / a;
    float t = t2-t1;
    if (t1 <= 0.0 && t2 <= 0.0)
	{
        return vec3(1.0);
    }


	if (t1 > 0.0 && t2 > 0.0)
	{
        return vec3(1.0 / (1.0+t * t * 50.0));
        //return vec3(1.0) - pow(Kr,vec3(1.0 / t));
	}

	return vec3(0.0);
}



// get the air density for a given height. Used 
float getAirDensity(float h)
{
	return exp(-h/200.0);
}

// get the amount of light scattered towards the eye when looking at target
// target is a terrain intersection
vec4 getInscatterTerrain(vec3 eye, vec3 target)
{
    vec3 p = eye;
    vec3 d = target-eye;
    float l = length(target-eye);
    vec3 c = vec3(0.0);
    
	//float distFactor = 0.01;
	float distFactor = sampleDistanceFactor;

    float alpha = dot(normalize(d), sunVector);
    float mie_factor = phase(alpha,miePhase) * mieBrightness * nearMieBrightness;
    // mie brightness
	float raleigh_factor = phase(alpha,-0.01) * raleighBrightness;
    // raleigh brightness
	//float adepth = adepthSky(vec3(0.0,0.9,0.0), sunVector);
	float skylight_factor = skylightBrightness;

	// get intensity of sky-light
	vec3 skyLight = textureLod(skyCubeTex,vec3(0.0,1.0,0.0),9).rgb;//getSkyLight(vec3(0.0,1.0,0.0));


	vec3 influx = sunIntensity();
    vec3 mie = vec3(0.0);
    vec3 cmie = vec3(0.0);
    vec3 raleigh = vec3(0.0);
	vec3 skyLightScatter = vec3(0.0);

    float t = 0.0;
    float dt = scatteringInitialStepSize;

	vec3 rp = vec3(texcoord0.xy * 497.0, hash(time*7.117));
	// dither start position
	t += dt * rand(rp);

	// dither step growth
	float stepgrowth = scatteringStepGrowthFactor * (1.0 + 0.04 * rand(rp));

	// offset eye by a small amount
	//eye += (vec3( hash(dot(rp, vec3(14.7, 13.5, 99.2))), hash(dot(rp, vec3(14.7, 13.5, 99.2)) * 391.7), hash(dot(rp, vec3(14.7, 13.5, 99.2)) * 173.1) ) - vec3(0.5)) * 0.2;

	//float height_scale = 6000.0;

    //for(float t=0.0;t<1.0;t+=dt)
	while (t < 1.0)
	{
        p = eye + d * t;
        float dist = l * t * distFactor;


		// no cloud influx
		float s = getShadow(p);// * cloudSunAbsorb(p);
        vec3 pointInflux = influx * s;

		float scatter_factor = dt;// * getAirDensity(p.y); // scatter less as we go higher.

        mie += absorb(dist, pointInflux, scatterAbsorb) * scatter_factor;
		raleigh += absorb(dist, Kral * pointInflux, scatterAbsorb) * scatter_factor;

		skyLightScatter += inscatter(dist, skyLight, scatterAbsorb) * scatter_factor * getAOInfluence(p);


        t+=dt;
        dt *= stepgrowth;
    }


	//mie *= dt;
	mie *= mie_factor * l * distFactor;
    //cmie *= (0.6 + 0.4 * cloud_mie_factor) * l * distFactor;
    //raleigh *= dt;
	raleigh *= raleigh_factor * l * distFactor;
	skyLightScatter *= skylight_factor * l * distFactor;

	float outScatter = dot(skyLightScatter,vec3(0.333));

    return vec4((mie + cmie + raleigh + skyLightScatter),1.0);
    // * Er;
}




float LinearizeDepth(float z)
{
  float n = 0.1; // camera z near
  float f = 4000.0; // camera z far
  return (2.0 * n) / (f + n - z * (f - n));	
}

float LinearizeDepth2(float depth)
{
	float FarClipDistance = 4000.0;
	float NearClipDistance = 0.1;
	float ProjectionA = FarClipDistance / (FarClipDistance - NearClipDistance);
	float ProjectionB = (-FarClipDistance * NearClipDistance) / (FarClipDistance - NearClipDistance);

	// Sample the depth and convert to linear view space Z (assume it gets sampled as
	// a floating point value of the range [0,1])
	return ProjectionB / (depth - ProjectionA);
}


void main(void)
{
    vec2 p = texcoord0.xy;
	vec4 paramT = texture(paramTex,p);
    vec4 normalT = texture(normalTex,p);
    vec4 normalTL = texture(normalLargeScaleTex,p);
	float depth = texture(depthTex,p).r;

	vec4 projpos = vec4(texcoord0.x * 2.0 - 1.0, texcoord0.y * 2.0 - 1.0, depth*2.0-1.0, 1.0);
	//vec4 pos = inverse(pre_projection_matrix) * projpos;
	vec4 pos = pre_projection_matrix * projpos;
	pos.xyz /= pos.w;


    vec4 c = vec4(0.0,0.0,0.0,1.0);
    float hitType = normalT.a;
    
    vec3 normal = normalize(normalT.xyz - vec3(0.5));
	vec3 normalLargeScale = normalize(normalTL.xyz - vec3(0.5));

    vec2 shadowAO = texture(shadeTex,pos.xz * texel).rg;

	// blend terrain height over y for distant samples
	float h = sampleHeight(pos.xz);
	float d = length(pos.xyz - eyePos.xyz);
	pos.y = mix(pos.y, h, smoothstep(500.0, 1000.0,d));


	
    if (hitType > 0.6)
	{
        c.rgb = generateCol(pos.xyz,normal,normalLargeScale,paramT, eyePos, shadowAO.r, shadowAO.g);
		vec4 inst = getInscatterTerrain(eyePos,pos.xyz);
        c.rgb *= inst.a;
        c.rgb += inst.rgb;

	}
	else
	{
        if (hitType > 0.05)
		{
			vec3 skyDir = normal;
			c.rgb += getSkyColour(skyDir);
			vec4 inst = getInscatterTerrain(eyePos,eyePos + skyDir * nearScatterDistance);
			c.rgb *= inst.a;
            c.rgb += inst.rgb;
		}
		else
		{
            c = vec4(1.0,1.0,0.0,1.0);
        }
	}
	
	out_Colour = vec4(c.rgb,1.0);
}
