// assumes vars:
// groundLevel, earthAtmosphere, mieBrightness, raleighBrightness

float denormh(float hnorm)
{
	return max(hnorm-groundLevel,0.001)*earthAtmosphereRadius;
}

// bullshit hack, but close enough for low altitudes
float airRefractiveIndex(float hnorm)
{
	return 1.000293 - (1.0-1.0/(1.0+denormh(hnorm)*0.0002))*0.000293;
}

// bullshit hack
float NbyHeight(float hnorm)
{
	return 2.55E25 * exp(-0.00011 * denormh(hnorm));		
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


	return 1000000.0;
}

// returns 1 on intersection, 0 on no intersection
float intersectGround(vec3 eye, vec3 dir, float groundheight)
{
    float a = dot(dir, dir);
    float b = 2.0*dot(dir, eye);
    float c = dot(eye, eye) - groundheight*groundheight;
    float det = b*b-4.0*a*c;
    if (det<0.0) return 0.0;  // no solution = no intersection 
    float detSqrt = sqrt(det);
    a+=a;
    float t1 = (-b - detSqrt) / a;
    float t2 = (-b + detSqrt) / a;
    //float tmin = min(t1,t2);
    //float tmax = max(t1,t2);
	
    return step(0.0,max(t1,t2));  // return 1.0 if the max intersection is in front of us	
}


float adepthSkyGround(vec3 eye, vec3 dir, float groundheight)
{
    return min(adepthSky(eye,dir),adepthGround(eye,dir,groundheight));
}

// exponential absorbtion - @pyalot http://codeflow.org/entries/2011/apr/13/advanced-webgl-part-2-sky-rendering/
vec3 absorb(float dist, vec3 col, float f)
{
    return col - col * pow(Kr, vec3(f / dist));
}

vec3 absorb(float dist, vec3 col, vec3 K, float f)
{
    return col - col * pow(K, vec3(f / dist));
}

vec3 outscatter(float dist, vec3 col, float f)
{
    return col * pow(Kr, vec3(f / dist));
}



float airDensity(float hnorm)
{
	return 0.001224 * exp(denormh(hnorm) * 0.000105);
}

float airDensityNorm(float hnorm)
{
	return exp(denormh(hnorm) * 0.000105);
}


float pathAirMass(vec3 start, vec3 end)
{
	float densityStart = airDensity(length(start));
	float densityEnd = airDensity(length(end));
	return (densityStart + densityEnd) * 0.5 * length(end-start);
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

float intersectGroundSoft(vec3 eye, vec3 dir, float groundheight, float soft)
{
	float h = groundheight-pointRayDistance(eye,dir,vec3(0.0));
	
	return smoothstep(0.0,soft,h);
}
	
	
vec3 sunIntensity(vec3 p, vec3 sunVector, vec3 sunLight, float scatterAbsorb)
{
	float depth = adepthSky(p, sunVector);
	return absorb(depth, sunLight, scatterAbsorb);
	
	//float airmass = pathAirMass(p,p+sunVector*depth);
	//return absorb(airmass, sunLight, scatterAbsorb);
}


vec3 getSimpleScattering(vec3 eye, vec3 dir, vec3 sunVector, float scatterAbsorb, float maxDist)
{
	vec3 col = vec3(0);
	float dist = min(maxDist,adepthSkyGround(eye, dir, groundLevel));
	vec3 p = eye + dir * dist;

	float airDensityAtEye = airDensityNorm(length(eye));	
	float airDensityAtP = airDensityNorm(length(p));	
	float totalAir = (airDensityAtP + airDensityAtEye) * 0.5 * dist;
	
		float alpha = dot(dir,sunVector);
		float ral = phase(alpha,ralPhase) * raleighBrightness;
		float mie = phase(alpha,miePhase) * mieBrightness * (1.0+totalAir);
		
		
		// calculate incoming light to point p
		
		// calculate atmospheric depth towards sun from p
		float adepthSun = max(0.0,adepthSky(p,sunVector));
		
		// calculate lowest altitude of sun ray to p
		float minAltitude = pointRayDistance(p,sunVector,vec3(0.0));
		
		// air density of lowest point
		float airDensityOfIncomingSun = airDensityNorm(minAltitude);
		
		vec3 sunAtP = absorb(adepthSun * airDensityOfIncomingSun, sunLight, scatterAbsorb);
		
		
		float groundHitHard = (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.001));
		float groundHitSoft = (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.01));
		
		// absorb along path
		vec3 influxAtP = sunAtP * groundHitSoft;
		
		// add some light to fake multi-scattering
		//vec3 additionalInflux = absorb(1.0,sunAtP*Kr * (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.3)),scatterAbsorb);
		//additionalInflux += absorb(dist*16.0,sunAtP * Kr * (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.5)),scatterAbsorb);

		vec3 additionalInflux = absorb(dist*2.0,sunAtP * Kr * (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.1)),scatterAbsorb);
		additionalInflux += absorb(dist*16.0,sunAtP * Kr * (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.5)),scatterAbsorb);
	
		//col += influxAtP * dist * dt;
		
		vec3 lightToEye = vec3(0.0);
		
		// sun disk
		lightToEye += influxAtP * smoothstep(0.99983194915,0.99983194915+0.00002,dot(dir,sunVector)) * dist * groundHitHard;
		
		// mie to eye
		lightToEye += mie * influxAtP * dist * groundHitHard;
		
		influxAtP += additionalInflux;
		
		// rayleigh to eye
		lightToEye += ral * influxAtP * Kr * dist;
		
		// additional
		//lightToEye += additionalInflux * dist * dt;
		
	
		
		col += absorb(dist , lightToEye, 1.0-totalAir);
	
	
	return col;
}





// =====================================================================================================================================

vec3 getRayMarchedScattering(vec3 eye, vec3 dir2, vec3 sunVector, float scatterAbsorb, float minDistance, float maxDistance)
{
	vec3 col = vec3(0);
	
	float dist = min(maxDistance,adepthSkyGround(eye, dir2, groundLevel));
	
	// advance minDistance along ray
	eye = eye + dir2 * minDistance;
	dist = max(0.00000001,dist-minDistance);
	
	
	//float dd = pointRayDistance(eye + dir * .006,sunVector,vec3(0.0));
	//return vec3((dd-groundLevel)*100.,(dd-groundLevel)*200.,(dd-groundLevel)*1000.);
	

	// get influx
	//vec3 influx = sunIntensity(eye,sunVector,sunLight,scatterAbsorb);

	
	// get approximate integrated air density over path
	//float totalAirMass = pathAirMass(eye,eye+dir*dist);
		
	
	
	float dt = 0.05;
	//float previousDensity = airDensity(length(eye)) * dist * dt;
	
	float riPrev = airRefractiveIndex(length(eye));
	
	vec3 p = eye;
	//float aboveGround = 1.0;
	
	float distmul = 1.0;
	
	float totalAir = 0.0;
	vec3 dir = dir2;
	
	//float t0 = minDistance / dist;
	
	
	for (float t=0.0;t<=1.0;t+=0.05)
	{
		//vec3 p = eye + dir * dist * t;
		p += dir * dist * dt;
		
		//float nearBoundary = step(minDistance, dist * t);
		
		//if (length(p)<groundLevel) aboveGround = 0.0;
		
		float airDensityAtP = airDensityNorm(length(p));
		totalAir += airDensityAtP * dist * dt;
		//float densityRatio = airdensity / previousDensity;
		//previousDensity = airdensity;
		
		//col += airDensityAtP*dist*dt*10.; 
		
		
		// refraction in air
		float riCurrent = airRefractiveIndex(length(p));
		dir = normalize(mix(dir,refract(dir,normalize(p),riPrev/riCurrent),0.001));
		riPrev = riCurrent;
		
		distmul *= riCurrent;
		
		float alpha = dot(dir,sunVector);
		float ral = phase(alpha,ralPhase) * raleighBrightness;
		float mie = phase(alpha,miePhase) * mieBrightness * (1.0+totalAir);
		
		
		// calculate incoming light to point p
		
		// calculate atmospheric depth towards sun from p
		float adepthSun = max(0.0,adepthSky(p,sunVector));
		
		// calculate lowest altitude of sun ray to p
		float minAltitude = pointRayDistance(p,sunVector,vec3(0.0));
		
		// air density of lowest point
		float airDensityOfIncomingSun = airDensityNorm(minAltitude);
		
		vec3 sunAtP = absorb(adepthSun * airDensityOfIncomingSun, sunLight, scatterAbsorb);
		
		
		float groundHitHard = (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.001));
		float groundHitSoft = (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.01));
		
		// absorb along path
		vec3 influxAtP = sunAtP * groundHitSoft;
		
		// add some light to fake multi-scattering
		vec3 additionalInflux = absorb(dist*2.0,sunAtP * Kr * (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.1)),scatterAbsorb);
		additionalInflux += absorb(dist*16.0,sunAtP * Kr * (1.0 - intersectGroundSoft(p, sunVector, groundLevel,0.5)),scatterAbsorb);
		
		//col += influxAtP * dist * dt;
		
		vec3 lightToEye = vec3(0.0);
		
		// sun disk
		lightToEye += influxAtP * smoothstep(0.99983194915,0.99983194915+0.00002,dot(dir,sunVector)) * dist * dt * groundHitHard;
		
		// mie to eye
		lightToEye += mie * influxAtP * dist * dt * groundHitHard;
		
		influxAtP += additionalInflux;
		
		// rayleigh to eye
		lightToEye += ral * influxAtP * Kr * dist * dt;
		
		// additional
		//lightToEye += additionalInflux * dist * dt;
		
	
		
		col += absorb(dist * t, lightToEye, 1.0-totalAir);// * nearBoundary;
		
			
	}
	
	return col;
}


