/*
	Functions for atmospheric scattering
*/
/*
	@geofftnz

	Just mucking around with some fake scattering. 
	Trying to come up with a nice-looking raymarched solution for atmospheric 
	scattering out to the edge of the atmosphere, plus a fast non-iterative version
	for nearer the viewer.

	Some code pinched from here: http://glsl.heroku.com/e#17563.3
	and here: http://codeflow.org/entries/2011/apr/13/advanced-webgl-part-2-sky-rendering/
*/
//|base

#define PI 3.1415927

// expects:
//float earthAtmosphereRadius = 6450.0;
//float groundLevel = 0.995;
//float mieBrightness = 0.02;
//float rayleighBrightness = 5.0;
//float miePhase = 0.97;
//float rayleighPhase = -0.01;
//float skyPrecalcBoundary;
//vec3 Kr = vec3(0.1287, 0.2698, 0.7216);
//vec3 sunLight = vec3(1.0,0.98,0.9)*10.0;

float earthAtmosphereHeight = earthAtmosphereRadius * (1.0 - groundLevel);



// constants for atmospheric scattering

vec3 Kral4 = vec3(2.1381376E-25,9.150625E-26,4.100625E-26);

// normalized radius to altitude in m
float radiusToAltitude(float hnorm)
{
	return max(hnorm-groundLevel,0.0000001)*earthAtmosphereRadius*1000.0;
}
// normalized radius to distance from centre of earth, in m.
float denorm(float n)
{
	return n*earthAtmosphereRadius*1000.0;
}

// bullshit hack, but close enough for low altitudes
float airRefractiveIndex(float hnorm)
{
	return 1.000293 - (1.0-1.0/(1.0+radiusToAltitude(hnorm)*0.0002))*0.000293;
}

// bullshit hack
float NbyHeight(float hnorm)
{
	return 2.55E25 * exp(-0.00011 * radiusToAltitude(hnorm));		
}


vec3 TotalRayleigh(float hnorm)
{
	
	float n = airRefractiveIndex(hnorm);
	float N = NbyHeight(hnorm);
	
	float n2 = n*n-1.0;
	n2 *= n2;
	
	return (248.05 * n2) / (3.0 * N * Kral4);   // The rayleigh scattering coefficient


    //  8PI^3 * (n^2 - 1)^2 * (6 + 3pn)     8PI^3 * (n^2 - 1)^2
    // --------------------------------- = --------------------  
    //    3N * Lambda^4 * (6 - 7pn)          3N * Lambda^4         
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

float adepthGround(vec3 eye, vec3 dir, float groundLevel)
{
    float a = dot(dir, dir);
    float b = 2.0*dot(dir, eye);
    float c = dot(eye, eye) - groundLevel*groundLevel;
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


	return 1000000.0;
}

// returns 1 on intersection, 0 on no intersection
float intersectGround(vec3 eye, vec3 dir, float groundLevel)
{
    float a = dot(dir, dir);
    float b = 2.0*dot(dir, eye);
    float c = dot(eye, eye) - groundLevel*groundLevel;
    float det = b*b-4.0*a*c;
    if (det<0.0) return 0.0;  // no solution = no intersection 
    float detSqrt = sqrt(det);
    a+=a;
    float t1 = (-b - detSqrt) / a;
    float t2 = (-b + detSqrt) / a;
	
    return step(0.0,max(t1,t2));  // return 1.0 if the max intersection is in front of us	
}


float adepthSkyGround(vec3 eye, vec3 dir, float groundLevel)
{
    return min(adepthSky(eye,dir),adepthGround(eye,dir,groundLevel));
}

// exponential absorbtion - @pyalot http://codeflow.org/entries/2011/apr/13/advanced-webgl-part-2-sky-rendering/
vec3 absorb(float dist, vec3 col, float f)
{
    return col - col * pow(Kr, vec3(f / max(dist,0.0000000001)));
}

vec3 absorb(float dist, vec3 col, vec3 K, float f)
{
    return col - col * pow(K, vec3(f / dist));
}

vec3 outscatter(float dist, vec3 col, float f)
{
    return col * pow(Kr, vec3(f / dist));
}

const float airDensityFalloff = -0.000105*3.0;  //-0.000105
const float airDensityFactor = 50.0;

float airDensityNorm(float hnorm)
{
	return exp(radiusToAltitude(hnorm) * airDensityFalloff) * airDensityFactor;
}

float airDensityDenorm(float h)
{
	return exp(h * airDensityFalloff) * airDensityFactor;
}

//float airDensity(float hnorm)
//{
//	return 0.001224 * airDensityNorm(hnorm);
//}

float airDensityIntegralDenorm(float h)
{
	return (-1.0 / airDensityFalloff) * (1.0 - exp(h * airDensityFalloff)) * airDensityFactor;
	//return (1.0 / -airDensityFalloff) * (exp(h * -airDensityFalloff));
}

float pathAirMassSphericalEstimate(vec3 start, vec3 end)
{
	float densityStart = airDensityNorm(length(start));
	float densityEnd = airDensityNorm(length(end));
	return (densityStart + densityEnd) * 0.5 * length(end-start);
}

float pathAirMassFlatLinear(vec3 start, vec3 end)
{
	float densityStart = airDensityNorm(length(start));
	float densityEnd = airDensityNorm(length(end));
	return (densityStart + densityEnd) * 0.5 * length(end-start);
}

float pathAirMassFlat(vec3 start, vec3 end)
{
	float h0 = radiusToAltitude(start.y);
	float h1 = radiusToAltitude(end.y);
	float dh = h1-h0; 
	float dist = length(end-start); 

	// horizontal ray - use linear approximation to avoid div by zero
	if (abs(dh) < 0.1){
		return (airDensityDenorm(h0) + airDensityDenorm(h1)) * dist * 0.5;
	}

	float air0 = airDensityIntegralDenorm(h0);
	float air1 = airDensityIntegralDenorm(h1);

	return (air1-air0) * (dist / dh);
}

float pathAirMassSpherical(vec3 start, vec3 end)
{
	float h0 = radiusToAltitude(length(start));
	float h1 = radiusToAltitude(length(end));
	float dh = h1-h0; 
	float dist = length(end-start);

	// horizontal ray - use linear approximation to avoid div by zero
	if (abs(dh) < 0.001){
		return airDensityDenorm(h0) * dist;
	}

	float air0 = airDensityIntegralDenorm(h0);
	float air1 = airDensityIntegralDenorm(h1);

	return (air1-air0) * (dist / dh);
}


float pointLineDistance(vec3 p, vec3 v, vec3 q)
{
	vec3 u = q-p;
	vec3 puv = v * (dot(v,u) / length(v));
	vec3 qq = p + puv;
	return length(q-qq);
}

float pointRayDistance(vec3 p, vec3 v, vec3 q)
{
	vec3 u = q-p;
	float cosvu = dot(v,u);
	if (cosvu<0.0) return length(q-p);
	vec3 puv = v * (cosvu / length(v));
	vec3 qq = p + puv;
	return length(q-qq);
}

float intersectGroundSoft(vec3 eye, vec3 dir, float groundLevel, float soft)
{
	float h = groundLevel-pointRayDistance(eye,dir,vec3(0.0));
	return smoothstep(0.0,soft,h);
}
	
	
vec3 sunIntensity(vec3 p, vec3 sunVector, vec3 sunLight, float scatterAbsorb)
{
	float depth = adepthSky(p, sunVector);
	return absorb(depth, sunLight, scatterAbsorb);
}

// simple single-step scattering approximation
// designed to be a cheaper implementation than raymarched scattering
// all parameters should be normalized to the r=1 sphere
//
//
vec3 getSimpleScattering(vec3 eye, vec3 dir, vec3 sunVector, float scatterAbsorb, float maxDist, float mieFactor, float airDensityFactor)
{
	vec3 col = vec3(0);
	float dist = min(maxDist,adepthSkyGround(eye, dir, groundLevel));
	vec3 p = eye + dir * dist;

	//float airDensityAtEye = airDensityNorm(length(eye));	
	//float airDensityAtP = airDensityNorm(length(p));	
	//float totalAir = (airDensityAtP + airDensityAtEye) * 0.5 * dist;
	float totalAir = pathAirMassFlat(eye,p) * airDensityFactor;
	
		float alpha = dot(dir,sunVector);
		float ral = phase(alpha,rayleighPhase) * rayleighBrightness * totalAir;
		float mie = phase(alpha,miePhase) * mieBrightness * totalAir;
		
		
		// calculate incoming light to point p
		
		// calculate atmospheric depth towards sun from p
		float adepthSun = max(0.0,adepthSky(eye,sunVector));

		float groundHitHard = (1.0 - intersectGroundSoft(eye, sunVector, groundLevel,0.001));
		float groundHitSoft = (1.0 - intersectGroundSoft(eye, sunVector, groundLevel,0.01));
		
		// calculate lowest altitude of sun ray to p
		//float minAltitude = pointRayDistance(p,sunVector,vec3(0.0));
		
		// air density of lowest point
		//float airDensityOfIncomingSun = airDensityNorm(minAltitude);

		// air density over path
		//float airDensityOfIncomingSun = pathAirMassFlat(p,p+sunVector*0.01);
		//vec3 sunAtP = absorb(adepthSun * airDensityOfIncomingSun, sunLight, scatterAbsorb);

		// bugs in here:
		float totalAirToSun = 0.0;
		/*
		for(float t=0;t<1.0;t+=0.1)
		{
			vec3 p2 = p+dir*dist*t;
			totalAirToSun += pathAirMassFlat(p2,p2+sunVector*adepthSun) * (1.0 - intersectGroundSoft(p2, sunVector, groundLevel,0.001)) * 0.1;
		}*/

		//totalAirToSun = pathAirMassFlat(p,p+sunVector*adepthSun);
		totalAirToSun = (pathAirMassSpherical(eye,eye+sunVector*(adepthSun+0.001)) + pathAirMassFlat(eye,eye+sunVector*(adepthSun+0.001))) * 0.5; 
		
		vec3 sunAtP = absorb(totalAirToSun, sunLight, scatterAbsorb);
		
		
		
		// absorb along path
		vec3 influxAtP = sunAtP * groundHitSoft;
		
		// add some light to fake multi-scattering
		vec3 additionalInflux = absorb(1.0,sunAtP*Kr * (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.1)),scatterAbsorb);
		additionalInflux += absorb(dist*16.0,sunAtP * Kr * (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.5)),scatterAbsorb);
	
		//col += influxAtP * dist * dt;
		
		vec3 lightToEye = vec3(0.0);
		
		// sun disk
		//lightToEye += influxAtP * sunLight * smoothstep(0.99983194915,0.99983194915+0.00002,dot(dir,sunVector)) * dist * groundHitHard;
		
		// mie to eye
		lightToEye += mie * influxAtP * dist * groundHitHard * mieFactor;
		
		//influxAtP += additionalInflux;
		
		// rayleigh to eye
		lightToEye += ral * influxAtP * Kr * dist;
		
		// additional
		//lightToEye += additionalInflux * dist * dt;
		
	
		
		//col += absorb(dist , lightToEye, 1.0-totalAir);
		col += absorb(totalAir, lightToEye, scatterAbsorb);
	
	return col;
}



vec3 getSunInflux(vec3 p, vec3 sunVector)
{
	// calculate atmospheric depth towards sun from p
	float adepthSun = max(0.0,adepthSky(p,sunVector));
		
	// calculate lowest altitude of sun ray to p
	//float minAltitude = pointRayDistance(p,sunVector,vec3(0.0));
	//float airDensityOfIncomingSun = airDensityNorm(minAltitude);
	//float airMassOnSunPath = adepthSun * airDensityOfIncomingSun;
	float airMassOnSunPath = pathAirMassSpherical(p,p+sunVector*adepthSun);
		
	return absorb(airMassOnSunPath, sunLight, scatterAbsorb);

}

// =====================================================================================================================================

vec3 getRayMarchedScattering(vec3 eye, vec3 dir2, vec3 sunVector, float scatterAbsorb, float minDistance, float maxDistance)
{
	vec3 col = vec3(0.0);
	
	float dist = min(maxDistance,adepthSkyGround(eye, dir2, groundLevel)) - minDistance;
	
	// advance minDistance along ray
	eye = eye + dir2 * minDistance;
	dist = max(0.00000001,dist-minDistance);

	float dt = 0.05;
	
	float riPrev = airRefractiveIndex(length(eye));
	
	vec3 p = eye;
	
	float distmul = 1.0;
	
	float totalAir = 0.0; 
	vec3 dir = dir2;
	
	for (float t=0.0;t<=1.0;t+=0.05)
	{
		p = eye + dir * dist * t;
		
		float airDensityAtP = airDensityNorm(length(p));
		float airMassOverSegment = airDensityAtP * dist * dt;
		totalAir += airMassOverSegment;
		
		// refraction in air
		float riCurrent = airRefractiveIndex(length(p));
		dir = normalize(mix(dir,refract(dir,normalize(p),riPrev/riCurrent),0.001));
		riPrev = riCurrent;
		
		distmul *= riCurrent;
		
		float alpha = dot(dir,sunVector);
		float ral = phase(alpha,rayleighPhase) * rayleighBrightness * airMassOverSegment;
		float mie = phase(alpha,miePhase) * mieBrightness * airMassOverSegment;
		
		// calculate incoming light to point p
		vec3 sunAtP = getSunInflux(p,sunVector);
		
		
		float groundHitHard = (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.001));
		float groundHitSoft = (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.01));
		
		// absorb along path
		vec3 influxAtP = sunAtP * groundHitSoft;
		
		// add some light to fake multi-scattering
		vec3 additionalInflux = absorb(airMassOverSegment*2.0,sunAtP * Kr * (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.1)),scatterAbsorb);
		additionalInflux += absorb(airMassOverSegment*16.0,sunAtP * Kr * (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.5)),scatterAbsorb);
		
		//col += influxAtP * dist * dt;
		
		vec3 lightToEye = vec3(0.0);
		
		// sun disk
		//lightToEye += influxAtP * smoothstep(0.99983194915-0.00001,0.99983194915+0.00001,dot(dir,sunVector)) * dist * dt * groundHitHard * 50.0;
		
		// mie to eye
		lightToEye += mie * influxAtP * dist * dt * groundHitHard;
		
		influxAtP += additionalInflux;
		
		// rayleigh to eye
		lightToEye += ral * influxAtP * Kr * dist * dt;
		
		col += absorb(totalAir, lightToEye, scatterAbsorb);
			
	}
	
	return col;
}

//|Terrain
// this code assumes it's running in the global lighting shader

float getShadow(vec3 p)
{
    return smoothstep(-0.05,-0.02,p.y - texture(shadeTex,p.xz * texel).r);
}

vec3 normpos(vec3 p, float scale)
{
	return ((p * scale) / (earthAtmosphereRadius)) + vec3(0.0,groundLevel,0.0);
}

// scale = size of terrain unit (texel) in km
vec3 getTerrainRaymarchScattering(vec3 eye, vec3 dir, vec3 sunVector, float scatterAbsorb, float dist, float scale, float nearAirFactor)
{
	vec3 c = vec3(0.0);

	// raymarch as series of t1-t2 segments.

	float alpha = dot(dir,sunVector);
	float ral = phase(alpha,rayleighPhase) * rayleighBrightness * 0.004 * nearAirFactor;
	float mie = phase(alpha,miePhase) * mieBrightness  * 0.001 * nearAirFactor; 
	
	// solar influx to viewer - used as influx for entire ray
	vec3 eyenorm = normpos(eye,scale);
	float adepthSun = max(0.0,adepthSky(eyenorm,sunVector));
	//float totalAirToSun = pathAirMassSpherical(eyenorm,eyenorm+sunVector*adepthSun);
	float totalAirToSun = pathAirMassFlat(eyenorm,eyenorm+sunVector*adepthSun);
	vec3 sunInflux = absorb(totalAirToSun, sunLight, scatterAbsorb);
	
	//return vec3(1.0,0.0,0.0) * sunInflux * 0.1;

	float prevShadow = getShadow(eye);
	vec3 p1 = eye;
	
	float n = hash(time + hash(dir.x) + hash(dir.y) + hash(dir.z));
	float totalAir = 0.0;
	 
	for (float t = 0; t < 1.0; t += 0.05)
	{
		float t1 = t + n * 0.045;
		float t1sq = t1 * t1;
		float t2 = min(1.0,t1 + 0.05);
		float t2sq = t2 * t2;
		float dt = t2sq - t1sq;
		float dtlen = dt * dist;
		float t2len = t2 * dist;
		
		vec3 p2 = eye + dir * t2sq * dist;
		
		//vec3 p2norm = normpos(p2,scale);
		//adepthSun = max(0.0,adepthSky(p2norm,sunVector));
		//totalAirToSun = pathAirMassSpherical(p2norm,p2norm+sunVector*adepthSun);
		//sunInflux = absorb(totalAirToSun, sunLight, scatterAbsorb);
		
		
		//float segmentAirMass = pathAirMassFlat(normpos(p1,scale),normpos(p2,scale));
		float segmentAirMass = airDensityDenorm(p2.y * scale * 1000.0) * dtlen * (scale / (earthAtmosphereRadius)) * nearAirFactor;
		totalAir += segmentAirMass;
		
		//float shadow = (getShadow(p) + prevShadow) * 0.5;
		float shadow = getShadow(p2);
		
		// contains fudge factor of 0.01
		//c += absorb(totalAir,sunInflux * shadow * segmentAirMass, scatterAbsorb);
		vec3 sun = (sunInflux * shadow * segmentAirMass);
		vec3 cseg =  sun * mie;
		cseg += sun * Kr * ral;
		c += absorb(totalAir, cseg, scatterAbsorb); 
		
		prevShadow = shadow;
		p1 = p2;
	}

	//c *= mie;

	return c;
}