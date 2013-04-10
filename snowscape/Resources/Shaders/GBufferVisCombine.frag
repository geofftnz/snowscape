#version 140
precision highp float;

uniform sampler2D posTex;
//uniform sampler2D normalTex;
uniform sampler2D paramTex;
uniform sampler2D heightTex;
uniform sampler2D shadeTex;

uniform vec4 boxparam;
uniform vec3 eyePos;
uniform vec3 sunVector;

uniform float minHeight;
uniform float maxHeight;

uniform float exposure;

in vec2 texcoord0;
out vec4 out_Colour;


vec3 getInscatterSky(vec3 eye, vec3 dir);

// air absorbtion
vec3 Kr = vec3(0.18867780436772762, 0.4978442963618773, 0.6616065586417131);
vec3 Kr2 = vec3(0.2334,0.8947,0.9552); // just making shit up

// raleigh scattering constants - maybe
vec3 Kral = vec3(2.284, 3.897, 8.227) * 0.2;

// inverse eye response - for mapping atmospheric scattering amounts to something more realistic
vec3 Er = vec3(0.6,0.6,1.0);

mat3 m = mat3( 0.00,  0.80,  0.60,
              -0.80,  0.36, -0.48,
              -0.60, -0.48,  0.64 );


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
    f  = 0.5000*noise( p ); p = m*p*2.02;
    f += 0.2500*noise( p ); p = m*p*2.03;
    f += 0.1250*noise( p ); p = m*p*2.01;
    f += 0.0625*noise( p );
    return f;
}

float texel = 1.0 / boxparam.x;

float sampleHeight(vec2 posTile)
{
	return texture2D(heightTex,posTile * texel).r;
}

float sampleHeightNoise(vec2 posTile, float f, float a)
{
	return sampleHeight(posTile) + fbm(vec3(posTile.xy*f,0.0)) * a;
}

// pos in tile coords (0-boxparam.xy)
vec3 getNormal(vec2 pos)
{
    float h1 = sampleHeight(vec2(pos.x, pos.y - 0.5));
	float h2 = sampleHeight(vec2(pos.x, pos.y + 0.5));
    float h3 = sampleHeight(vec2(pos.x - 0.5, pos.y));
	float h4 = sampleHeight(vec2(pos.x + 0.5, pos.y));

    return normalize(vec3(h3-h4,1.0,h1-h2));
}

vec3 getNormalNoise(vec2 pos, float f, float a)
{
    float h1 = sampleHeightNoise(vec2(pos.x, pos.y - 0.5),f,a);
	float h2 = sampleHeightNoise(vec2(pos.x, pos.y + 0.5),f,a);
    float h3 = sampleHeightNoise(vec2(pos.x - 0.5, pos.y),f,a);
	float h4 = sampleHeightNoise(vec2(pos.x + 0.5, pos.y),f,a);

    return normalize(vec3(h3-h4,1.0,h1-h2));
}


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
	return col - col * pow(Kr2, vec3(1.0 / dist));
	//vec3 k = pow(Kr, vec3(f / dist));
	//return col - col * k;
	//return col - col * k;
}


vec3 getSkyColour(vec3 skyvector)
{
	vec3 skycol = 
		mix(
			vec3(0.02,0.03,0.2),
			vec3(0.4,0.6,0.9),
			pow(clamp(1.0-dot(skyvector,vec3(0.0,1.0,0.0)),0.0,1.0),2.0)
			);

	// scattering around the sun
	//skycol += vec3(1.0,0.9,0.3) * pow(clamp(dot(skyvector,sunVector),0.0,1.0),300.0) * 4.0;

	// sun disk
	skycol += vec3(1.0,0.9,0.6) * smoothstep(0.9998,0.99995,dot(skyvector,sunVector)) * 8.0;

	return skycol;
}

float getShadowForGroundPos(vec3 p, float shadowHeight)
{
	return smoothstep(-2.0,-0.1,p.y - shadowHeight);
}

float getShadow(vec3 p)
{
	return smoothstep(-2.0,-0.1,p.y - texture2D(shadeTex,p.xz * texel).r);
}

float directIllumination(vec3 p, vec3 n, float shadowHeight)
{
	return  getShadowForGroundPos(p, shadowHeight) * clamp(dot(n,sunVector)+0.2,0,1);
}

vec3 eyeToSkyCoords(vec3 eye)
{
	float rearth = 6371000.0;
	float hsky = 50000;
	float hterrainunit = 4;

	float terraintosky = hterrainunit / (rearth + hsky);
	float groundbase = rearth / (rearth + hsky);

	return eye * terraintosky + vec3(0.0,groundbase,0.0); // eye position scaled so that radius of earth + atmosphere = 1
}


// this is assumed to be constant across the entire terrain, because the terrian is small compared to the atmosphere
// this should be moved to a uniform
vec3 sunIntensity()
{
	return absorb(adepthSky(vec3(0.0,0.9,0.0), sunVector), vec3(1.0), 1.0); // 28.0
}


vec3 terrainDiffuse(vec3 p, vec3 n, vec4 s, float shadowHeight)
{
	vec3 colH1 = pow(vec3(0.3,0.247,0.223),vec3(2.0));
	vec3 colL1 = pow(vec3(0.41,0.39,0.16),vec3(2.0));
	vec3 colW = pow(vec3(0.7,0.8,1.0),vec3(2.0));

	float looseblend = s.r*s.r;

	vec3 col = mix(colH1,colL1,looseblend);

	vec3 eyeDir = normalize(p-eyePos);
	vec3 wCol = vec3(0.1,0.2,0.25) + getSkyColour(reflect(eyeDir,n)) * smoothstep(-2.0,-0.1,p.y - shadowHeight);

	vec3 colW0 = wCol;  // blue water
	//vec4 colW0 = vec4(0.4,0.7,0.95,1.0);  // blue water
	vec3 colW1 = vec3(0.659,0.533,0.373);  // dirty water
	vec3 colW2 = vec3(1.2,1.3,1.4); // white water

	colW = mix(colW0,colW1,clamp(s.b*1.5,0,1));  // make water dirty->clean

	float waterblend = smoothstep(0.02,0.1,s.g) * 0.1 + 0.4 * s.g * s.g;

	col = mix(col,colW,waterblend); // water

    // misc vis
	vec3 colE = vec3(0.4,0.6,0.9);
	col += colE * clamp(s.a,0.0,1.0);

	return col;
}

float sampleDistanceFactor = 0.0009765625 * 0.5;
float sampleDistanceExponent = 4.0;

vec3 getSkyLight(vec3 dir)
{
	return getInscatterSky(vec3(0.0,0.0,0.0),dir);
}

vec3 generateCol(vec3 p, vec3 n, vec4 s, vec3 eye, float shadowHeight, float AO)
{
	//vec3 col = terrainDiffuse(p,n,s,shadowHeight);
	vec3 col = vec3(0.5);

	//float diffuse = directIllumination(p,n,shadowHeight);
	//col = col * diffuse + col * vec3(0.8,0.9,1.0) * 0.7 * AO;

	vec3 col2 = 
			col * sunIntensity() * clamp(dot(n,sunVector)+0.1,0,1) * getShadowForGroundPos(p,shadowHeight) 
			+ col * getSkyLight(n) * AO;
		
	return col2;
	// can probably ignore the aborption between point and eye
	//return absorb(adepthTerrain(eye, p) * sampleDistanceFactor,col2,sampleDistanceExponent);
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

float horizonBlock(vec3 eye, vec3 dir, float groundheight)
{
	float a = dot(dir, dir);
	float b = 2.0 * dot(dir, eye);
	float c = dot(eye, eye) - groundheight*groundheight;
	float det = b*b - 4.0*a*c;	

	if (det < 0.0) // no intersection
	{
		return 1.0; 
	}

	det = sqrt(det);
	a += a;
	float t1 = (-b - det) / a;
	float t2 = (-b + det) / a;

	if (t1 >= 0.0 || t2 >= 0.0)
	{
		return 1.0;
	}


	return 0.0;
}




// get the amount of light scattered towards the eye when looking at target
// target is a terrain intersection
vec3 getInscatterTerrain(vec3 eye, vec3 target)
{
	vec3 p = eye;
	vec3 d = target-eye;
	float l = length(target-eye);
	vec3 c = vec3(0.0);

	float alpha = dot(normalize(d), sunVector);
	float mie_factor = phase(alpha,0.99) * 0.2;  // mie brightness
	float raleigh_factor = phase(alpha,-0.01) * 0.5;  // raleigh brightness
	//float adepth = adepthSky(vec3(0.0,0.9,0.0), sunVector);

	vec3 influx = sunIntensity();

	vec3 mie = vec3(0.0);
	vec3 raleigh = vec3(0.0);

	float t = 0.0;
	float dt = 0.001;

	//for(float t=0.0;t<1.0;t+=dt)
	while (t < 1.0)
	{
		p = eye + d * t;

		float dist = l * t;

		float s = getShadow(p) * dt;

		mie += absorb(dist * sampleDistanceFactor, influx, sampleDistanceExponent) * s;
		raleigh += absorb(dist * sampleDistanceFactor, Kral * influx, sampleDistanceExponent) * s;
		
		// light / shadow factor
		//float s = getShadow(p);
		//c += vec3(0.4,0.6,0.9) * s * 0.0001;
		t+=dt;
		dt *= 1.08;
	}

	//mie *= dt;
	mie *= mie_factor * l * sampleDistanceFactor;

	//raleigh *= dt;
	raleigh *= raleigh_factor * l * sampleDistanceFactor;
	
	return (mie + raleigh);// * Er;
}

float groundlevel = 0.990;

// eye is eye in world coordinates - this will be normalised for the sky sphere
// dir is the direction of the ray
vec3 getInscatterSky(vec3 eye, vec3 dir)
{
	highp float tscale = 0.004/6000.0;
	vec3 p0 =  eye * tscale + vec3(0.0,0.991,0.0); // replace with scaled pos and height

	//if (horizonBlock(p0, -dir, groundlevel) < 0.5)
	//{
		//return vec3(0.0); // todo: raycast and scatter
	//}

	//return vec3(adepthSkyGround(p0,dir,groundlevel));


	float ray_length = adepthSkyGround(p0,dir,groundlevel); //adepthSky(p0, dir);

	float alpha = dot(dir, sunVector);
	float mie_factor = phase(alpha,0.99) * 0.01;  // mie brightness
	float raleigh_factor = phase(alpha,-0.01) * 0.2;  // raleigh brightness

	vec3 mie = vec3(0.0);
	vec3 raleigh = vec3(0.0);

	float nsteps = 50.0;
	float stepsize = 1.0 / nsteps;
	float step_length = ray_length / nsteps;
	vec3 sunIntensity = vec3(1.0);

	float scatteramount = 50.0;
	float ralabsorbfactor = 140.0;
	float mieabsorbfactor = 260.0;
	

	for(float t = 0.0; t < 1.0; t += stepsize)
	{
		float sample_dist = ray_length * t;
		vec3 p = p0 + dir * sample_dist;
		
		float sample_depth = adepthSky(p,sunVector);

		vec3 influx = absorb(sample_depth, sunIntensity, scatteramount);// * horizonLight(p,sunVector,groundlevel,scatteramount);

		raleigh += absorb(sample_dist, Kral * influx, ralabsorbfactor);
		mie += absorb(sample_dist, influx, mieabsorbfactor) * horizonBlock(p, -dir, groundlevel);
	}

	raleigh *= raleigh_factor * ray_length;
	mie *= mie_factor * ray_length;

	return vec3(raleigh + mie);
}



void main(void)
{
	vec4 c = vec4(0.0,0.0,0.0,1.0);
	
	vec2 p = texcoord0.xy;
	vec4 posT = texture2D(posTex,p);
	float hitType = posT.a;
	vec4 pos = vec4(posT.xyz + eyePos,0.0);
	//vec4 normalT = texture2D(normalTex,p);
	vec4 paramT = texture2D(paramTex,p);
	//vec3 normal = normalize(normalT.xyz - 0.5);

	vec3 wpos = pos.xyz - eyePos;

	float smoothness = smoothstep(0.02,0.1,paramT.g)*8.0 + paramT.r*paramT.r * 2.0;
	
	vec3 normal = getNormalNoise(pos.xz,0.76,1.0 / (1.0+smoothness));
	//vec3 normal = getNormal(pos.xz);

	vec2 shadowAO = texture2D(shadeTex,pos.xz * texel).rg;

	float d = length(wpos);

	if (hitType > 0.6)
	{
	
		c.rgb = generateCol(pos.xyz,normal,paramT, eyePos, shadowAO.r, shadowAO.g);	
		//c.rgb = sunIntensity();

		//c.rb = 0.0;
		//c.g = adepthSky(vec3(0.0,0.99,0.0), sunVector);

		//c = vec4(0.0,0.0,0.0,1.0);
		
		
		//c.rgb += getInscatterTerrain(eyePos,pos.xyz);

		//vec4 fogcol = vec4(0.6,0.8,1.0,1.0);
		//c = mix(c,fogcol,getInscatterTerrain(eyePos,pos.xyz).r);

		//vec4 fogcol = vec4(0.6,0.8,1.0,1.0);
		//d /= 1024.0;
		//float fogamount = 1.0 / (exp(d * d * 0.2));
//
		//if (hitType < 0.5){
			//fogamount = 0.0;
		//}
//
		//c = mix(fogcol,c,fogamount);
		//
		//c.r = shadowAO.r;
		//c.g = shadowAO.g;
		//c.rgb = vec3(shadowAO.g * 0.4 + 0.9 * directIllumination(pos.xyz,normal, shadowAO.r));

		// visualize normal
		//c = vec4(normal*0.5+0.5,1.0);

		// visualize eye direction vector
		//c = vec4(normalize(pos.xyz - eyePos)*0.5+0.5,1.0);
	}
	else
	{
		if (hitType > 0.05)
		{

			//vec3 l = normalize(vec3(0.4,0.6,0.2));
			
			//vec3 skycol = getSkyColour(normalize(posT.xyz));
			//c = vec4(skycol,1.0);


			//vec3 boxMin = vec3(-1024.0,minHeight,-1024.0);
			//vec3 boxMax = vec3(2048.0,maxHeight,2048.0);
//
//
			//if (eyePos.x >= boxMin.x && eyePos.y >= boxMin.y && eyePos.z >= boxMin.z &&
				//eyePos.x < boxMax.x && eyePos.y <= boxMax.y && eyePos.z < boxMax.z)
			//{
//
				//// intersect ray with bounds box
				//vec3 skyDir = normalize(posT.xyz);
				//float boxt = intersectBoxInside(eyePos, skyDir, boxMin,boxMax);
//
				//c.rgb += getInscatterTerrain(eyePos,eyePos + skyDir * boxt); // target a sphere around the terrain for the initial pass
			//}
//
//1.0 – exp(-fExposure x color)
			c.rgb += getInscatterSky(eyePos, normalize(posT.xyz));
			//c.rgb += vec3(1.0) - exp(getInscatterSky(eyePos, normalize(posT.xyz)) * -1.2f);
			



			//vec4 skycol = mix(vec4(0.6,0.8,1.0,1.0),vec4(0.1,0.1,0.4,1.0),clamp(dot(posT.xyz,vec3(0.0,-1.0,0.0)),0.0,1.0));
			//c = mix(skycol,vec4(1.0),pow(clamp(dot(posT.xyz,-sunVector),0.0,1.0),50.0));

			// visualize eye direction vector
			//c = vec4(posT.xyz*0.5+0.5,1.0);
		}
		else
		{
			c = vec4(1.0,1.0,0.0,1.0);
		}
	}



	/*
	vec2 p = texcoord0.xy * 2.0;
	// split screen into 4
	if (p.x < 1.0)
	{
		if (p.y < 1.0)
		{
			vec3 pos = texture2D(posTex,p).xyz + eyePos;
			c.rgb = pos.xyz / 1024.0;
		}
		else
		{
			c = texture2D(normalTex,p-vec2(0.0,1.0));
		}
	}
	else
	{
		if (p.y < 1.0)
		{
			c = vec4(0.0);
		}
		else
		{
			c = texture2D(paramTex,p-vec2(1.0,1.0));
		}
	}
	*/

	// fog

	// exposure
	c.rgb = vec3(1.0) - exp(c.rgb * exposure);  // -1.2

	//out_Colour = vec4(c.rgb,1.0);
    out_Colour = vec4(sqrt(c.rgb),1.0);
	//out_Colour = vec4(pow(c.rgb,vec3(0.45)),1.0);
}
