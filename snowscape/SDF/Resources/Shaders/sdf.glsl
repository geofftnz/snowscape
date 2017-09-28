//|vs
#version 140

uniform mat4 inverse_projectionview_matrix;
uniform vec3 eyePos;

in vec3 vertex;

out vec4 eyeTarget;

void main() {
	gl_Position = vec4(vertex.xy,0.9999999,1.0);

	eyeTarget = inverse_projectionview_matrix * vec4(vertex.x, -vertex.y, 1.0, 1.0);
	eyeTarget /= eyeTarget.w;
}


//|fs
#version 140
precision highp float;

uniform vec3 eyePos;
uniform float iGlobalTime;
uniform float alpha;
uniform float wheel;
uniform float showTraceDepth;

in vec4 eyeTarget;

out vec4 out_Col;

#include "Include/Noise/noise.glsl"



const float MAXDIST = 1000.0;

#define HYBRID_RAYMARCH 1
#define FOG_ENABLE 1



vec3 randDir(float randseed)
{
	//seed = hash(seed * 7.35654) * 65464.0;

	randseed += hash(randseed + 6548.31);

	return vec3
	(
		hash(randseed * 7.35),
		hash(randseed * 3.35),
		hash(randseed * 9.35)
	) * 2.0 - 1.0;
}
vec3 randDirNormal(float randseed)
{
	return normalize(randDir(randseed));
}

// signed distance to sphere
float sdSphere(vec3 p, float r)
{
	return length(p) - r;
}

// sphere intersection with early-exit
float sdSphere(vec3 p, vec3 ro, vec3 rd, float r)
{
	float dist = sdSphere(p,r);  // get general SDF because we'll need it later

	// if we haven't been given ro & rd, use the general SDF
	if (rd.x<-1.5) return dist;

	// check to see if we're on/in sphere
	if (dist<=0.0) return dist;

    float a = dot(rd, rd);
    float b = 2.0*dot(rd, p);
    float c = dot(p,p) - r*r;
    float det = b*b-4.0*a*c;
	if (det<0.0) return MAXDIST; // ray does not hit sphere
	
	float detSqrt = sqrt(det);
    a+=a;
    float t1 = (-b - detSqrt) / a;
    float t2 = (-b + detSqrt) / a;

	if (t1<0.0 && t2<0.0) return MAXDIST;
	return t1;
}

// bumpy sphere
float sdBumpySphere(vec3 p, float r, float noise_scale, float noise_amp)
{
	float lp = length(p);

	// early exit if we're outside noise radius
	if (lp > r + noise_amp * 3.0) return lp - (r + noise_amp * 2.0);
	
	float r2 = r + noise(normalize(p) * noise_scale) * noise_amp;
	return lp - r2;
}
float sdBumpySphere(vec3 p, vec3 ro, vec3 rd, float r0, float noise_scale, float noise_amp)
{
	// if we haven't been given ro & rd, use the general SDF
	if (rd.x<-1.5) return sdBumpySphere(p,r0,noise_scale,noise_amp);

	// check to see if we're on/in sphere
	float dist = sdSphere(p,r0);
	if (dist <= noise_amp * 2.1 ) return sdBumpySphere(p,r0,noise_scale,noise_amp);

	// use a larger radius for the raytrace
	float r = r0 + noise_amp * 2.0;
	// intersect outer sphere and early fail
    float a = dot(rd, rd);
    float b = 2.0*dot(rd, p);
    float c = dot(p,p) - r*r;
    float det = b*b-4.0*a*c;
	if (det<0.0) return MAXDIST; // ray does not hit sphere

	float detSqrt = sqrt(det);
    a+=a;
    float t1 = (-b - detSqrt) / a;
    float t2 = (-b + detSqrt) / a;

	if (t1<0.0 && t2<0.0) return MAXDIST;  // no intersection
	return t1 + noise_amp * 0.1;  // lie about distance to get us inside

}


float max3(vec3 a)
{
	return max(a.x,max(a.y,a.z));
}
float max3(vec3 a, vec3 b)
{
	return max(max(a.x,b.x),max(max(a.y,b.y),max(a.z,b.z)));
}
float min3(vec3 a, vec3 b)
{
	return min(min(a.x,b.x),min(min(a.y,b.y),min(a.z,b.z)));
}


float sdCylindery(vec3 p, float r, float h)
{
	float d = length(p.xz)-r;
	return max(d,abs(p.y)-h);
}

float sdCylinderx(vec3 p, float r, float h){ return sdCylindery(p.yzx,r,h); }
float sdCylinderz(vec3 p, float r, float h){ return sdCylindery(p.zxy,r,h); }

// general ray-plane intersection
float sdPlane(vec3 p, vec3 ro, vec3 rd, vec4 plane)
{
	// early exit
	// if the origin point is below the plane, bail out
	if ((dot(plane.xyz,ro)+plane.w) < 0.0) return MAXDIST;

	// at this point we know we started above the plane.
	// if the ray is directed away from the plane, we can never intersect
	float d = -dot(rd,plane.xyz);
	if (d<=0.0) return MAXDIST;

	return (dot(plane.xyz,p)+plane.w) / d;
}

// general ray-plane distance
float sdPlane(vec3 p, vec4 plane)
{
	//float d = dot(rd,plane.xyz);
	//if (d<=0.0) return 100000000.0;

	// distance from a plane P=xyzw to point q is given by dot(P.xyz,q)+P.w
	// we want the distance from the point to the plane, so it's negative.
	return (dot(plane.xyz,p)+plane.w);
}

// ray to axis-aligned-plane distance
float sdPlanex(vec3 p, float h){return p.x - h;}
float sdPlaney(vec3 p, float h){return p.y - h;}
float sdPlanez(vec3 p, float h){return p.z - h;}
float sdPlanexn(vec3 p, float h){return h - p.x;}
float sdPlaneyn(vec3 p, float h){return h - p.y;}
float sdPlanezn(vec3 p, float h){return h - p.z;}

// ray to axis-aligned-plane intersection
float sdPlanex(vec3 p, vec3 ro, vec3 rd, float h)
{
	// if we don't have a valid ray direction supplied or we're below the plane, use the regular distance function
	if (rd.x < -1.5 || ro.x < h) return sdPlanex(p,h); 

	// at this point we know we started above the plane.
	// if the ray is directed away from the plane, we can never intersect
	if (rd.x>0.0) return MAXDIST;

	// intersect
	return (h-p.x) / rd.x;
}
float sdPlaney(vec3 p, vec3 ro, vec3 rd, float h)
{
	if (rd.x < -1.5 || ro.y < h) return sdPlaney(p,h); 
	if (rd.y>0.0) return MAXDIST;
	return (h-p.y) / rd.y;
}
float sdPlanez(vec3 p, vec3 ro, vec3 rd, float h)
{
	if (rd.x < -1.5 || ro.z < h) return sdPlanez(p,h); 
	if (rd.z>0.0) return MAXDIST;
	return (h-p.z) / rd.z;
}

// ray to inverted axis-aligned-plane intersection
float sdPlanexn(vec3 p, vec3 ro, vec3 rd, float h)
{
	// if we don't have a valid ray direction supplied or we're below the plane, use the regular distance function
	if (rd.x < -1.5 || ro.x > h) return sdPlanexn(p,h); 

	// at this point we know we started above the plane.
	// if the ray is directed away from the plane, we can never intersect
	if (rd.x<0.0) return MAXDIST;

	// intersect
	return (h-p.x) / rd.x;
}
float sdPlaneyn(vec3 p, vec3 ro, vec3 rd, float h)
{
	if (rd.x < -1.5 || ro.y > h) return sdPlaneyn(p,h); 
	if (rd.y<0.0) return MAXDIST;
	return (h-p.y) / rd.y;
}
float sdPlanezn(vec3 p, vec3 ro, vec3 rd, float h)
{
	if (rd.x < -1.5 || ro.z > h) return sdPlanezn(p,h); 
	if (rd.z<0.0) return MAXDIST;
	return (h-p.z) / rd.z;
}

float dinter(float a, float b)
{
	return max(a,b);
}

float sdBox(vec3 p, vec3 b)
{
	vec3 d = abs(p) - b;
	return length(max(d,0.)) + max3(min(d,0.));
}

// partially broken
float sdBox2(vec3 p, vec3 ro, vec3 rd, vec3 b)
{
	if (rd.x < -1.5) return sdBox(p,b); 

	return 
		max(dinter(sdPlanex(p,ro,rd,b.x),sdPlanexn(p,ro,rd,-b.x)),
		max(dinter(sdPlaney(p,ro,rd,b.y),sdPlaneyn(p,ro,rd,-b.y)),
		    dinter(sdPlanez(p,ro,rd,b.z),sdPlanezn(p,ro,rd,-b.z))
		));

	
	//vec3 d = abs(p) - b;
	//return length(max(d,0.)) + max3(min(d,0.));
}


float sdBox(vec3 p, vec3 ro, vec3 rd, vec3 b)
{
	// no direction supplied, or we're inside the box
	if (rd.x < -1.5 || (all(greaterThanEqual(p,-b)) && all(lessThanEqual(p,b)))) return sdBox(p,b); 

	float tmin = -MAXDIST, tmax = MAXDIST;
	vec3 bmin = -b - p;
	vec3 bmax = b - p;

	vec3 t1 = bmin/rd;
	vec3 t2 = bmax/rd;

	tmin = max(tmin,min(t1.x,t2.x));
	tmax = min(tmax,max(t1.x,t2.x));
	tmin = max(tmin,min(t1.y,t2.y));
	tmax = min(tmax,max(t1.y,t2.y));
	tmin = max(tmin,min(t1.z,t2.z));
	tmax = min(tmax,max(t1.z,t2.z));

	if (tmax < tmin || tmin < 0.0) return MAXDIST;
	return tmin;

}


// returns the parameter with the smallest x component
vec2 dunion(vec2 a, vec2 b)
{
	return a.x<b.x?a:b;
}


vec2 dinter(vec2 a, vec2 b)
{
	return a.x>b.x?a:b;
}

vec2 dsubtract(vec2 a, vec2 b)
{
	float d = max(-b.x,a.x);
	return vec2(d,a.y);
}

// radiused combiner - modified from cupe/mercury
vec2 dcombine(vec2 a, vec2 b, float r)
{
	vec2 m = dunion(a,b);
	if (a.x < r && b.x < r)
	{
		return vec2(min(m.x,r-length(vec2(r-a.x,r-b.x))),b.y);
	}
	return m;
}
// radiused combiner - modified from cupe/mercury
vec2 dcombine2(vec2 a, vec2 b, float r)
{
	vec2 m = dunion(a,b);
	if (a.x < r && b.x < r)
	{
		return vec2(min(m.x,a.x*b.x),b.y);
	}
	return m;
}

// creates an object from an id and distance
vec2 ob(float hitid, float d)
{
	return vec2(d,hitid);
}

// modulus on 1 axis, returns instance id
float pmod1(inout float p, float m)
{
	float m2 = m * 0.5;
	p = mod(p+m2,m)-m2;

	return floor((p+m2)/m);
}

// modulus on 2 axes, returns instance id
vec2 pmod2(inout vec2 p, vec2 m)
{
	vec2 m2 = m * 0.5;
	p = mod(p+m2,m)-m2;

	return floor((p+m2)/m);
}

// modulus on 3 axes, returns instance id
vec3 pmod3(inout vec3 p, vec3 m)
{
	vec3 m2 = m * 0.5;
	p = mod(p+m2,m)-m2;

	return floor((p+m2)/m);
}

//vec2 drepeatx()
//#define REPX(p,m,F) 


// translation
vec3 tr(vec3 p, vec3 t) { return p-t; }
vec3 tr_x(vec3 p, float t) { return tr(p,vec3(t,0.,0.)); }
vec3 tr_y(vec3 p, float t) { return tr(p,vec3(0.,t,0.)); }
vec3 tr_z(vec3 p, float t) { return tr(p,vec3(0.,0.,t)); }



vec2 de_boxcyl(vec3 p)
{
	vec2 s = vec2(100000000.0,-1);
	s = dunion(s,ob(0.5,sdBox(tr_x(p,-1.5),vec3(1.,2.,3.))));

	float cyl = sdCylinderz(p,0.5,1.0);

	s = dcombine( ob(0.5,sdBox(tr_x(p,-1.5),vec3(1.,2.,3.))) ,ob(1.0,cyl),0.2);

	s = dsubtract(s,ob(0.5,sdSphere(p-vec3(0.6,0.5,0.2),0.8)));

	return s;
}


// distance estimator
// returns distance bound in x, hit id in y
vec2 de(vec3 p, vec3 ro, vec3 dir)
{
	vec2 s = vec2(100000000.0,-1);

	//float m = 6.0;	pmod2(p.xz,vec2(m));

	//s = dunion(s,de_boxcyl(p));
	//s = dunion(s,de_boxcyl(p - vec3(m,0.0,0.0)));
	//s = dunion(s,de_boxcyl(p - vec3(0.0,0.0,m)));
	
	// hollow cube
	s = dunion(s, ob(0.0,sdBox(p,ro,dir,vec3(1.0))));
	s = dsubtract(s, ob(0.0,sdSphere(p,ro,dir,1.2)));
	s = dsubtract(s, ob(0.0,-sdSphere(p,ro,dir,1.21)));

	s = dunion(s, ob(0.0,sdSphere(tr_y(tr_x(p,-2.0),-0.3),tr_y(tr_x(ro,-2.0),-0.3),dir,0.5)));
	s = dunion(s, ob(0.0,sdBox(tr_x(p,2.0),tr_x(ro,2.0),dir,vec3(0.5))));

	// bumpy sphere
	s = dunion(s, ob(0.0,sdBumpySphere(p - vec3(3.0,0.0,1.0),ro - vec3(3.0,0.0,1.0),dir,1.0,5.0,0.1)));


	// tall buildings
	//vec3 p2 = p - vec3(2.0,0.0,1.0); pmod1(p2.z,2.5);
	//s = dunion(s, ob(0.0,sdBox(p2 - vec3(2.0,10.0,0.0),vec3(1.0,20.0,1.0))));
	s = dunion(s, ob(0.0,sdBox(p - vec3(-2.0,10.0,-1.0),ro - vec3(-2.0,10.0,-1.0),dir,vec3(1.0,20.0,1.0))));
	s = dunion(s, ob(0.0,sdBox(p - vec3(4.5,10.0,-5.0),ro - vec3(4.5,10.0,-5.0),dir,vec3(1.0,20.0,1.0))));
	s = dunion(s, ob(0.0,sdBox(p - vec3(6.9,10.0,2.0),ro - vec3(6.9,10.0,2.0),dir,vec3(1.0,20.0,1.0))));

	// ground plane
	s = dunion (s, ob(1.0,sdPlaney(p,ro,dir,-0.6)));
	//s = dunion (s, ob(1.0,sdPlaney(p,-0.5)));

	// sunken box
	s = dsubtract(s, ob(0.0,sdBox(p - vec3(-0.9,-20.0,1.2),ro - vec3(-0.9,-20.0,1.2),dir,vec3(1.0,20.0,1.0))));

	//s = dunion(s,ob(1.0,sdSphere(tr_x(p,1.0),0.5)));
	//s = dunion(s,ob(1.0,sdSphere(tr_x(p,2.0),0.75)));
	//s = dunion(s,ob(1.0,sdSphere(tr_x(p,3.0),1.0)));

	//s = dunion(s,ob(0.5,sdBox(tr_x(p,-1.5),vec3(1.,2.,3.))));
	//float cyl = sdCylinderz(p,0.5,1.0);
	//s = dcombine( ob(0.5,sdBox(tr_x(p,-1.5),vec3(1.,2.,3.))) ,ob(1.0,cyl),1.0);

	return s;
}

vec2 de(vec3 p)
{
	return de(p,vec3(0.0),vec3(-2.,0.,0.));
}


// normal via distance field gradient
const float deNormalEpsilon = 0.001;
vec3 deNormal(vec3 p, vec3 rd)
{
    vec3 e = vec3(deNormalEpsilon, 0, 0);
	p -= rd * (e * 1.1);
	float dist = de(p).x;
    return normalize(vec3(dist - de(p - e.xyy).x,
                        dist - de(p - e.yxy).x,
                        dist - de(p - e.yyx).x));
}



// raymarches the scene described by distance estimator de()
// returns distance in x, hit id in y, stepcount in z
vec3 intersectScene(vec3 ro, vec3 rd)
{
	float t=0.0;
	vec2 pdist = vec2(MAXDIST,-1.0);
	vec3 res = vec3(pdist,0.0);
	float epsilon = 0.0001; // tolerable error

	// check to see if we're inside the scene and bail out if we are.
	if (de(ro).x < 0.0) return res;

	for(int i=0;i<50.0;i++)
	{
		res.z = i;
		vec3 p = ro + rd * t;  // position along ray

		#ifdef HYBRID_RAYMARCH
		pdist = de(p,ro,rd); // get distance bound (hybrid SDF/raytrace)
		#else
		pdist = de(p); // get distance bound (pure SDF raymarch)
		#endif

		t += pdist.x; // move position

		if (pdist.x < epsilon)
		{
			break;
		}

		if (t > MAXDIST)
		{
			pdist.y = -1.0;
			break;
		}
		epsilon *= 1.0 + min(0.1,t * 0.001);

	}
	res.x = t;
	res.y = pdist.y;
	

	return res;
}

// shadow query - marches ray, returns light multiplier (0-1) for given direction
// early exit on collision
// credit: iq/rgba
float queryLight(vec3 ro, vec3 rd, float t0, float t1)
{
	for (float t = t0; t < t1;)
	{
		float d = (de( ro + rd * t).x);
		if (d<0.0005) return 0.0;
		t += d;
	}
	return 1.0;
}
// soft shadow query - marches ray, returns light multiplier (0-1) for given direction
// early exit on collision
// credit: iq/rgba
float queryLightSoft(vec3 ro, vec3 rd, float t0, float t1, float k)
{
	float res = 1.0;
	for (float t = t0; t < t1;)
	{
		float d = (de( ro + rd * t).x);
		if (d<0.0005) return 0.0;
		res =  min(res, (k*d)/t);
		t += d;
	}
	return res;
}

// ambient occlusion query
// marches ray in fixed steps from ro towards rd. 
// accumulates occlusion based on proximity to scene
float queryAO(vec3 ro, vec3 rd, float maxDistance)
{
	float t = 0.0;
	float dt = maxDistance / 5.0; 
	float ao = 0.0;
	float m = 2.0 / maxDistance;

	for(int i=0;i<5;i++)
	{
		float pdist = max(0.0,de(ro + rd * t).x); // get distance bound

		//ao *= (1.0 - 0.1 * (1.0 / (1.0 + 4.0 * pdist)));
		ao += max(0.0,(t - pdist)) * m;
		m*=0.5; 
		t += dt;

		//if (pdist<0.0) break;
	}
	
	return max(0.0,1.0 - ao);
}

vec3 skyDome(vec3 rd)
{
	vec3 col = mix(vec3(0.01,0.025,0.1),vec3(0.1,0.25,0.4),clamp(pow(1.0-rd.y,4.0),0.0,1.0));
	col = mix(col,vec3(0.2,0.35,0.4),clamp(pow(1.0-rd.y,10.0),0.0,1.0));
	col = mix(col,vec3(0.3,0.27,0.25),clamp(pow(1.0-rd.y,20.0),0.0,1.0));

	return col * 2.0;
}

// bounce light around the scene in a shitty attempt at path tracing.
// assumes an outdoor scene, so non-intersecting rays sample the sky dome
// assumes light source is at infinity with no attenuation
// pos = intersection point
// ro = original ray origin (may not be needed)
// rd = original ray direction
// normal = surface normal at intersection
vec3 diffuseBounceOutdoorParallel(vec3 pos, vec3 ro0, vec3 rd0, vec3 normal, vec3 light_dir, vec3 light_col, float iteration)
{
	vec3 col = vec3(0.0);
	vec3 diffuse_mul = vec3(1.0); // surface col
	vec3 p = pos;
	vec3 n = normal;
	vec3 ro,rd;
	float ii = 17.3f * iteration;


	for(int i=0;i<2;i++)
	{
		// generate new ray
		ro = p + n * 0.0001;
		rd = randDirNormal(  dot(ro,vec3(159.17,176.37,133.77)) * (3.0+hash(ii + iGlobalTime))   );
		ii += 5.17;
		
		float rmul = clamp(dot(n,rd),0.0,1.0);
		if (rmul < 0.0001) break;
		diffuse_mul *= rmul; // todo: * albedo of this hit

		// trace ray
		vec3 scene_hit = intersectScene(ro,rd);

		// no hit: sample skybox
		if (scene_hit.y < 0.0)
		{
			col += diffuse_mul * skyDome(rd);
			break;
		}
		else
		{
			// move position, calculate distance attenuation
			p = ro + rd * scene_hit.x;
			n = deNormal(p, rd);
			//diffuse_mul *= (1.0 / (1.0+ scene_hit.x * scene_hit.x));  // falloff
			diffuse_mul *= (1.0 / (1.0+ scene_hit.x));  // falloff

			// get direct lighting contribution from light source
			float light1 = queryLight(p + n * 0.001, light_dir,0.0,MAXDIST) * (clamp(dot(n,light_dir),0.0,1.0));

			// accumulate lighting
			col += diffuse_mul * light_col * light1;
		}

	}

	return col;
}

// intersects the scene and returns a colour in rgb, distance along ray in a.
vec4 shadeScene(vec3 ro, vec3 rd)
{
	vec3 col = vec3(0.0);
	//vec3 light_dir = normalize(vec3(0.3,0.5 + sin(iGlobalTime*0.2) * 0.3 ,0.7+ sin(iGlobalTime*0.3) * 0.1));
	vec3 light_dir = normalize(vec3(0.3,0.5 + sin(0.5) * 0.3 ,0.7+ sin(0.3) * 0.1));
	vec3 light_col = vec3(20.0);

	vec3 scene_hit = intersectScene(ro,rd);

	if (showTraceDepth>0.5)	return vec4(scene_hit.z * 0.02,0.0,0.0,scene_hit.x);
	
	if (scene_hit.y < 0.0)
	{ 
		scene_hit.x = MAXDIST;
		col = skyDome(rd);
	}
	else
	{
		vec3 pos = ro + rd * scene_hit.x;

		vec3 normal = deNormal(pos, rd);
		float light1 = queryLight(pos + normal * 0.001, light_dir,0.0,MAXDIST);
		//float light1 = queryLightSoft(pos + normal * 0.001, light_dir,0.0,MAXDIST,20.0);

		// Diffuse
		vec3 diffuse = vec3(1.0);//mix(vec3(1.0,0.5,0.0),vec3(0.5,0.0,0.8), scene_hit.y);
		col += diffuse * light_col * (clamp(dot(normal,light_dir),0.0,1.0)) * light1;

		// multi-bounce diffuse
		col += diffuseBounceOutdoorParallel(pos,ro,rd,normal,light_dir,light_col,1.0);
		col += diffuseBounceOutdoorParallel(pos,ro,rd,normal,light_dir,light_col,1.0);
		col += diffuseBounceOutdoorParallel(pos,ro,rd,normal,light_dir,light_col,1.0);
		//col += diffuseBounceOutdoorParallel(pos,ro,rd,normal,light_dir,vec3(1.0),2.0) * 0.25;
		//col += diffuseBounceOutdoorParallel(pos,ro,rd,normal,light_dir,vec3(1.0),3.0) * 0.25;
		//col += diffuseBounceOutdoorParallel(pos,ro,rd,normal,light_dir,vec3(1.0),4.0) * 0.25;

		// Specular
		float ior = 0.9;

		float r0 = pow((1.0-ior)/(1.0+ior),2.0);
		vec3 refl = reflect(rd, normal);
		vec3 halfangle = (rd + light_dir) / length(rd + light_dir);
		vec3 specular = vec3(0.5);
		float schlick = r0 + (1-r0) * pow(1.0 - max(0.0,dot(-halfangle, rd)), 5.0);

		//col += specular * pow(max(0.0,dot(refl, light_dir)),40.0) * schlick * light1;
		//col += specular * schlick * light1;

		// AO
		//col += vec3(0.1,0.25,0.4) * 0.5 * queryAO(pos , vec3(0.,1.,0.), 1.0);// AO traced upwards
		//col += vec3(0.1,0.25,0.4) * queryAO(pos , normal, 2.0);// AO traced outwards
		//col += vec3(0.1,0.25,0.4) * (normal.y * 0.1 + 0.9) * queryAO(pos , normal, 3.0);// AO traced outwards, with sky dome estimation
	}

	// sun
	float sun = smoothstep(0.99983194915 - 0.00005,0.99983194915+0.0002,dot(rd,light_dir)) * queryLight(ro, light_dir,0.0,MAXDIST);
	col += vec3(5.0) * sun;

	return vec4(col, scene_hit.x);
}


float intersectPlane(vec3 ro, vec3 rd, vec4 plane)
{
	float d = dot(rd,plane.xyz);
	if (d==0.0) return -1.0;
	return -(dot(ro,plane.xyz)+plane.w) / d;
}


float isoLine(float x, float t1)
{
	return 1.0-smoothstep(t1,t1*1.1,abs(mod(x+0.5,1.0)-0.5));
}
 
void main() 
{

	vec3 ro = eyePos;
	vec3 rd = normalize(eyeTarget.xyz - eyePos);

	vec3 col = vec3(rd*0.5+0.5);


	//vec2 scene_hit = intersectScene(ro,rd);
	vec4 scene_col = shadeScene(ro,rd);
	col = scene_col.rgb;
	
	float dist = scene_col.a;

	// intersect with plane
	float plane_t = intersectPlane(ro,rd,vec4(0.0,1.0,0.0,-wheel*0.1));

	if (plane_t > 0.0 && plane_t < scene_col.a)
	{
		vec3 plane_p = ro + rd * plane_t;
		
		#ifdef HYBRID_RAYMARCH
		float dist_estimate = (showTraceDepth>0.5) ? de(plane_p).x : de(plane_p, ro, rd).x;
		#else
		float dist_estimate = de(plane_p).x;
		#endif

		vec3 dcol1 = vec3(1.0,0.5,0.0);  // close
		vec3 dcol2 = vec3(1.0,0.02,0.05);  // medium
		vec3 dcol3 = vec3(0.1,0.02,0.2);  // far
		 
		float adist = sqrt(abs(dist_estimate));
		vec3 pcol = mix(dcol1, dcol2, clamp(adist,0.0,1.0));
		pcol = mix(pcol, dcol3, clamp((adist-1.0) * 0.5,0.0,1.0));

		if (dist_estimate < 0.0) pcol = vec3(1.0) - pcol;

		pcol *= 0.8;
		
		float isoline_intensity = 0.2 / (1.0 + plane_t*0.2);
		// distance field isolines
		float distance_isoline = max(isoLine(dist_estimate * 10.0,0.05),isoLine(dist_estimate,0.02));
		pcol += vec3(1.0,0.5,0.1) * (distance_isoline * isoline_intensity);

		// xz grid isolines
		//pcol += vec3(0.2,1.0,0.5) * (  max(isoLine(plane_p.x,0.01),isoLine(plane_p.x*10.0,0.02))  * isoline_intensity * 0.5);
		//pcol += vec3(0.2,0.5,1.0) * (  max(isoLine(plane_p.z,0.01),isoLine(plane_p.z*10.0,0.02)) * isoline_intensity * 0.5);

		col = mix(col, pcol, 0.8);
		dist = plane_t;
	}

	
	// fog
	float fog = 1.0 / max(1.0,exp(dist * 0.5));
	
	if (dist<MAXDIST && (showTraceDepth<0.5))	col = mix(vec3(0.0,0.0,0.0), col, fog);   //vec3(0.3,0.27,0.25)

	// Reinhardt tone map
	float whitelevel = 4.0;
	col.rgb = (col.rgb  * (vec3(1.0) + (col.rgb / (whitelevel * whitelevel))  ) ) / (vec3(1.0) + col.rgb);

	col = pow(col, vec3(1.0/2.2));  // gamma

	//todo: tonemap
	out_Col = vec4(col,1.0);
}




