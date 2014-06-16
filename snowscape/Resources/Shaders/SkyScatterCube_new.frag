#version 140
precision highp float;

uniform vec3 sunVector;
uniform float groundLevel;

uniform float raleighBrightness;
uniform float mieBrightness;
uniform float scatterAbsorb;
uniform vec3 Kr;
uniform vec3 eye;
uniform vec3 sunLight;

uniform vec3 facenormal;
uniform vec3 facexbasis;
uniform vec3 faceybasis;

in vec2 sky2d;

out vec3 out_Sky;


// Atmospheric scattering model
// Code adapted from Martins
// http://blenderartists.org/forum/showthread.php?242940-unlimited-planar-reflections-amp-refraction-%28update%29
// Martijn Steinrucken countfrolic@gmail.com

vec3 wPos;
//vec3 sunPos = vec3(0.,0.,1.);

vec3 sunDirection = sunVector;

float turbidity = 2.5;
float rayleighCoefficient = raleighBrightness;//2.5;

float mieCoefficient = 0.015;//mieBrightness; //0.015;
const float mieDirectionalG = 0.90;

// constants for atmospheric scattering
const float e = 2.71828182845904523536028747135266249775724709369995957;
const float pi = 3.141592653589793238462643383279502884197169;

const float n = 1.0003; // refractive index of air
const float N = 5.545E25; // number of molecules per unit volume for air at
						// 288.15K and 1013mb (sea level -45 celsius)

// wavelength of used primaries, according to preetham
const vec3 primaryWavelengths = vec3(680E-9, 550E-9, 450E-9);

// mie stuff
// K coefficient for the primaries
const vec3 K = vec3(0.686, 0.678, 0.666);
const float v = 4.0;

// optical length at zenith for molecules
const float rayleighZenithLength = 8.4E3;
const float mieZenithLength = 1.25E3;
const vec3 up = vec3(0.0, 1.0, 0.0);

float sunIntensity = dot(sunLight,vec3(1.0))*4;//1000.0;   // TODO: allow for sun light colour
const float sunAngularDiameterCos = 0.99983194915; // 66 arc seconds -> degrees, and the cosine of that

// earth shadow hack
const float cutoffAngle = pi/1.95;
const float steepness = 1.5;


vec3 TotalRayleigh(vec3 primaryWavelengths)
{
	vec3 rayleigh = (8.0 * pow(pi, 3.0) * pow(pow(n, 2.0) - 1.0, 2.0)) / (3.0 * N * pow(primaryWavelengths, vec3(4.0)));   // The rayleigh scattering coefficient
 
    return rayleigh; 

    //  8PI^3 * (n^2 - 1)^2 * (6 + 3pn)     8PI^3 * (n^2 - 1)^2
    // --------------------------------- = --------------------  
    //    3N * Lambda^4 * (6 - 7pn)          3N * Lambda^4         
}

float RayleighPhase(float cosViewSunAngle)
{	 
	return (3.0 / (16.0*pi)) * (1.0 + pow(cosViewSunAngle, 2.0));
}

vec3 totalMie(vec3 primaryWavelengths, vec3 K, float T)
{
	float c = (0.2 * T ) * 10E-18;
	return 0.434 * c * pi * pow((2.0 * pi) / primaryWavelengths, vec3(v - 2.0)) * K;
}

float hgPhase(float cosViewSunAngle, float g)
{
	return (1.0 / (4.0*pi)) * ((1.0 - pow(g, 2.0)) / pow(1.0 - 2.0*g*cosViewSunAngle + pow(g, 2.0), 1.5));
}

float SunIntensity(float zenithAngleCos)
{
	return sunIntensity * max(0.0, 1.0 - exp(-((cutoffAngle - acos(zenithAngleCos))/steepness)));
}



void main() 
{ 
	vec3 viewDir = normalize(facenormal + facexbasis * sky2d.x + faceybasis * sky2d.y);
	
    // General parameter setup
	vec3 finalColor = vec3(0.1);								// The background color, dark gray in this case

    // Cos Angles
    float cosViewSunAngle = dot(viewDir, sunDirection);
    float cosSunUpAngle = dot(sunDirection, up);
    float cosUpViewAngle = dot(up, viewDir);
    
    float sunE = SunIntensity(cosSunUpAngle);  // Get sun intensity based on how high in the sky it is

	// extinction (absorbtion + out scattering)
	// rayleigh coefficients
	//vec3 rayleighAtX = TotalRayleigh(primaryWavelengths) * rayleighCoefficient;
    vec3 rayleighAtX = vec3(5.176821E-6, 1.2785348E-5, 2.8530756E-5);
    
	// mie coefficients
	vec3 mieAtX = totalMie(primaryWavelengths, K, turbidity) * mieCoefficient;  
    
	// optical length
	// cutoff angle at 90 to avoid singularity in next formula.
	float zenithAngle = max(0.0, cosUpViewAngle);
    
	float rayleighOpticalLength = rayleighZenithLength / zenithAngle;
	float mieOpticalLength = mieZenithLength / zenithAngle;

	// combined extinction factor	
	vec3 Fex = exp(-(rayleighAtX * rayleighOpticalLength + mieAtX * mieOpticalLength));

	// in scattering
	vec3 rayleighXtoEye = rayleighAtX * RayleighPhase(cosViewSunAngle);
	vec3 mieXtoEye = mieAtX *  hgPhase(cosViewSunAngle, mieDirectionalG);
     
    vec3 totalLightAtX = rayleighAtX + mieAtX;
    vec3 lightFromXtoEye = rayleighXtoEye + mieXtoEye; 
    
    vec3 somethingElse = sunE * (lightFromXtoEye / totalLightAtX);
    
    vec3 sky = somethingElse * (1.0 - Fex);
	
    sky *= mix(
			vec3(1.0),
			pow(somethingElse * Fex,vec3(0.5)),
			clamp(pow(1.0-dot(up, sunDirection),5.0),0.0,1.0)
			);
			

	out_Sky = sky;

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
	float raleigh_factor = phase(alpha,-0.01) * raleighBrightness;
    // raleigh brightness

	vec3 mie = vec3(0.0);
    vec3 raleigh = vec3(0.0);
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
        raleigh += absorb(sample_dist, Kral * influx, ralabsorbfactor) * getAirDensity(p);
        //mie += absorb(sample_dist, influx, mieabsorbfactor) * horizonBlock(p, -dir, groundLevel);
    }

	raleigh *= raleigh_factor * ray_length;
    //mie *= mie_factor * ray_length;
    return vec3(raleigh + mie);
}


vec3 getInscatterSkyMulti(vec3 eye, vec3 dir)
{
    highp float tscale = 0.004/6000.0;
    vec3 p0 =  eye * tscale + vec3(0.0,groundLevel + 0.001,0.0);

	float ray_length = adepthSkyGround(p0,dir,groundLevel);

	float alpha = dot(dir, sunVector);
    float mie_factor = phase(alpha,0.99) * mieBrightness;
    // mie brightness
	float raleigh_factor = phase(alpha,-0.01) * raleighBrightness;
    // raleigh brightness

	vec3 mie = vec3(0.0);
    vec3 raleigh = vec3(0.0);
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
        raleigh += absorb(sample_dist, Kral * influx, ralabsorbfactor);
        //mie += absorb(sample_dist, influx, mieabsorbfactor) * horizonBlock(p, -dir, groundLevel);
    }

	raleigh *= raleigh_factor * ray_length;
    //mie *= mie_factor * ray_length;
    return vec3(raleigh + mie);
}




void main(void)
{

	// let's assume we're looking down the z axis
	// screen will be x,y

	// we're in a cube of radius 1.0
	// assume eye is at 0,0,0

	vec3 dir = normalize(facenormal + facexbasis * sky2d.x + faceybasis * sky2d.y);

	out_Sky = getInscatterSky(eye, dir);


	//if (dir.x > 0.0){
		//out_Sky = getInscatterSky(eye, dir);
	//}
	//else{
		//out_Sky = getInscatterSkyMulti(eye, dir);
	//}
//
}
*/