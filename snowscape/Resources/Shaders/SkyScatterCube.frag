#version 140
precision highp float;

uniform vec3 sunVector;
uniform float groundLevel;
uniform float rayleighBrightness;
uniform float mieBrightness;
uniform float miePhase;// = 0.97;
uniform float rayleighPhase;// = -0.01;
uniform float skyPrecalcBoundary;  // 16

uniform float scatterAbsorb;
uniform vec3 Kr;
uniform vec3 eye;
uniform vec3 sunLight;

uniform vec3 facenormal;
uniform vec3 facexbasis;
uniform vec3 faceybasis;

in vec2 sky2d;

out vec3 out_Sky;

// expected variables for atmospheric scattering
float earthAtmosphereRadius = 6450.0;


#include "atmospheric.glsl"


void main(void)
{

	// let's assume we're looking down the z axis
	// screen will be x,y

	// we're in a cube of radius 1.0
	// assume eye is at 0,0,0

	// set up boundary between near and far scattering
	float boundary = skyPrecalcBoundary / earthAtmosphereRadius;  // 4km

	vec3 dir = normalize(facenormal + facexbasis * sky2d.x + faceybasis * sky2d.y);

	out_Sky = getRayMarchedScattering(vec3(0.0,groundLevel+0.001,0.0), dir, sunVector, scatterAbsorb, boundary,10000.0);


	//if (dir.x > 0.0){
		//out_Sky = getInscatterSky(eye, dir);
	//}
	//else{
		//out_Sky = getInscatterSkyMulti(eye, dir);
	//}
//
}



/*
// other vars
vec3 Kr2 = Kr;
vec3 Kral = Kr;

// random
float rand(vec3 co){
    return fract(sin(dot(co.xyz ,vec3(12.9898,78.233,47.985))) * 43758.5453);
}

// random vec
vec3 randvec(vec3 p){
	return normalize(
		vec3(
			rand(p),
			rand(p+vec3(7.,17.,19.)),
			rand(p+vec3(3.,7.,13.))
			)
			-vec3(0.5)
		);
}


// exponential absorbtion - @pyalot http://codeflow.org/entries/2011/apr/13/advanced-webgl-part-2-sky-rendering/
vec3 absorb(float dist, vec3 col, float f)
{
    return col - col * pow(Kr2, vec3(f / dist));
}

vec3 absorb(float dist, vec3 col, vec3 K, float f)
{
    return col - col * pow(K, vec3(f / dist));
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

// fake refracted light
vec3 horizonLight(vec3 eye, vec3 dir, float groundheight, float factor)
{
	// intersect the eye-->dir ray with the radius=groundheight sphere centred on 0,0,0
	float a = dot(dir, dir);
    float b = 2.0 * dot(dir, eye);
    float c = dot(eye, eye) - groundheight*groundheight;
    float det = b*b - 4.0*a*c;

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


// get the air density for a given height. Exponential falloff.
float getAirDensity(vec3 p)
{
	float h = (length(p) - groundLevel) / (1.0 - groundLevel);
	return exp(h * -3.0);
}


vec3 getInscatterSky(vec3 eye, vec3 dir)
{
    highp float tscale = 0.004/6000.0;
    vec3 p0 =  eye * tscale + vec3(0.0,groundLevel + 0.001,0.0);

	float ray_length = adepthSkyGround(p0,dir,groundLevel);

	float alpha = dot(dir, sunVector);
    float mie_factor = phase(alpha,0.99) * mieBrightness;
    // mie brightness
	float rayleigh_factor = phase(alpha,-0.01) * rayleighBrightness;
    // rayleigh brightness

	vec3 mie = vec3(0.0);
    vec3 rayleigh = vec3(0.0);
    float nsteps = 50.0;
    float stepsize = 1.0 / nsteps;
    float step_length = ray_length / nsteps;
    vec3 sunIntensity = sunLight;
    float scatteramount = scatterAbsorb;

	float ralabsorbfactor = 140.0;
    float mieabsorbfactor = 260.0;

    // calculate fake refraction factor. This will be used to shift the sampling points along the ray to simulate light curving through the atmosphere.
	float refk = pow(1.0 - clamp(  abs(0.05 + dot(dir,normalize(p0))) ,0.0,1.0),9.0) * 0.5;

	for(float t = 0.0; t < 1.0; t += stepsize)
	{
        float sample_dist = ray_length * t;
        vec3 p = p0 + dir * sample_dist;

        // advance sun sample position along ray proportional to how shallow our eye ray is.
		vec3 psun = p0 + dir * (t * (1.0-refk) + refk) * ray_length;
        float sample_depth = adepthSky(psun,sunVector) + sample_dist;
        // todo: + sample_dist ?

		vec3 influx = absorb(sample_depth, sunIntensity, scatteramount) * horizonLight(psun,sunVector,groundLevel,scatteramount);
        rayleigh += absorb(sample_dist, Kral * influx, ralabsorbfactor) * getAirDensity(p);
        //mie += absorb(sample_dist, influx, mieabsorbfactor) * horizonBlock(p, -dir, groundLevel);
    }

	rayleigh *= rayleigh_factor * ray_length;
    //mie *= mie_factor * ray_length;
    return vec3(rayleigh + mie);
}


vec3 getInscatterSkyMulti(vec3 eye, vec3 dir)
{
    highp float tscale = 0.004/6000.0;
    vec3 p0 =  eye * tscale + vec3(0.0,groundLevel + 0.001,0.0);

	float ray_length = adepthSkyGround(p0,dir,groundLevel);

	float alpha = dot(dir, sunVector);
    float mie_factor = phase(alpha,0.99) * mieBrightness;
    // mie brightness
	float rayleigh_factor = phase(alpha,-0.01) * rayleighBrightness;
    // rayleigh brightness

	vec3 mie = vec3(0.0);
    vec3 rayleigh = vec3(0.0);
    float nsteps = 50.0;
    float stepsize = 1.0 / nsteps;
    float step_length = ray_length / nsteps;
    vec3 sunIntensity = sunLight;
    float scatteramount = scatterAbsorb;

	float ralabsorbfactor = 140.0;
    float mieabsorbfactor = 260.0;

    // calculate fake refraction factor. This will be used to shift the sampling points along the ray to simulate light curving through the atmosphere.
	float refk = pow(1.0 - clamp(  abs(0.05 + dot(dir,normalize(p0))) ,0.0,1.0),9.0) * 0.5;

	for(float t = 0.0; t < 1.0; t += stepsize)
	{
        float sample_dist = ray_length * t;
        vec3 p = p0 + dir * sample_dist;

        // advance sun sample position along ray proportional to how shallow our eye ray is.
		vec3 psun = p0 + dir * (t * (1.0-refk) + refk * 1.0) * ray_length;
        float sample_depth = adepthSky(psun,sunVector) + sample_dist;
        // todo: + sample_dist ?

		// do another scattering step
		vec3 secondaryScattering = vec3(0.0);
		for (float sstep=0.;sstep<5.;sstep+=1.0){
			vec3 dir2 = randvec(p*19. + vec3(dir*sstep));
			secondaryScattering += getInscatterSky(p,dir2);
		}

		vec3 sunIntensity2 = sunIntensity + secondaryScattering * 16.;

		vec3 influx = absorb(sample_depth, sunIntensity2, scatteramount) * horizonLight(psun,sunVector,groundLevel,scatteramount);
        rayleigh += absorb(sample_dist, Kral * influx, ralabsorbfactor);
        //mie += absorb(sample_dist, influx, mieabsorbfactor) * horizonBlock(p, -dir, groundLevel);
    }

	rayleigh *= rayleigh_factor * ray_length;
    //mie *= mie_factor * ray_length;
    return vec3(rayleigh + mie);
}
*/



