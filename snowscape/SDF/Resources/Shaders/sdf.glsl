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

in vec4 eyeTarget;

out vec4 out_Col;


float sdSphere(vec3 p, float r)
{
	return length(p) - r;
}

float max3(vec3 a)
{
	return max(a.x,max(a.y,a.z));
}

float sdBox(vec3 p, vec3 b)
{
	vec3 d = abs(p) - b;
	return length(max(d,0.)) + max3(min(d,0.));
}

float sdCylindery(vec3 p, float r, float h)
{
	float d = length(p.xz)-r;
	return max(d,abs(p.y)-h);
}

float sdCylinderx(vec3 p, float r, float h){ return sdCylindery(p.yzx,r,h); }
float sdCylinderz(vec3 p, float r, float h){ return sdCylindery(p.zxy,r,h); }

float sdPlanex(vec3 p, float h){return p.x - h;}
float sdPlaney(vec3 p, float h){return p.y - h;}
float sdPlanez(vec3 p, float h){return p.z - h;}

float sdPlanex(vec3 p, vec3 d, float h)
{
	if (d.x < -1.0) return sdPlanex(p,h);
	if (d.x == 0.0) return 100000000.0; // ray is parallel to plane - return large number
	return (h-p.x)/d.x;
}
float sdPlaney(vec3 p, vec3 d, float h)
{
	if (d.x < -1.5) return sdPlaney(p,h);
	if (d.y == 0.0) return 100000000.0; // ray is parallel to plane - return large number
	float t = ((h-p.y)/d.y);
	if (t<-0.01) return 10000000.0; // ray is pointing away from plane, will never intersect
	return t;
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
vec2 de(vec3 p, vec3 dir)
{
	vec2 s = vec2(100000000.0,-1);

	//float m = 3.0;	pmod2(p.xz,vec2(m));

	//s = dunion(s,de_boxcyl(p));
	//s = dunion(s,de_boxcyl(p - vec3(m,0.0,0.0)));
	//s = dunion(s,de_boxcyl(p - vec3(0.0,0.0,m)));
	
	s = dunion(s, ob(0.0,sdBox(p,vec3(1.0))));
	s = dsubtract(s, ob(0.0,sdSphere(p,1.2)));
	s = dsubtract(s, ob(0.0,-sdSphere(p,1.22)));

	s = dunion(s, ob(0.0,sdSphere(tr_x(p,-2.0),0.5)));
	s = dunion(s, ob(0.0,sdBox(tr_x(p,2.0),vec3(0.5))));

	// ground plane
	s = dunion (s, ob(1.0,sdPlaney(p,dir,-0.6)));
	//s = dunion (s, ob(1.0,sdPlaney(p,-0.5)));

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
	return de(p,vec3(-2.,0.,0.));
}


// normal via distance field gradient
const float deNormalEpsilon = 0.001;
vec3 deNormal(vec3 p, vec3 rd)
{
    vec3 e = vec3(deNormalEpsilon, 0, 0);
	p -= rd * (e * 1.1);
	float dist = de(p);
    return normalize(vec3(dist - de(p - e.xyy).x,
                        dist - de(p - e.yxy).x,
                        dist - de(p - e.yyx).x));
}

const float MAXDIST = 1000.0;

// raymarches the scene described by distance estimator de()
// returns distance in x, hit id in y
vec2 intersectScene(vec3 ro, vec3 rd)
{
	float t=0.0;
	vec2 pdist = vec2(MAXDIST,-1.0);
	vec2 res = pdist;
	float epsilon = 0.00005; // tolerable error

	// check to see if we're inside the scene and bail out if we are.
	if (de(ro).x < 0.0) return res;

	for(int i=0;i<150.0;i++)
	{
		vec3 p = ro + rd * t;  // position along ray
		pdist = de(p,rd); // get distance bound

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
		if (d<0.001) return 0.0;
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
		if (d<0.001) return 0.0;
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
	float dt = maxDistance / 10.0; 
	float ao = 1.0;

	for(int i=0;i<10;i++)
	{
		vec3 p = ro + rd * t;
		t += dt;
		float pdist = max(0.0,de(p).x); // get distance bound

		ao *= (1.0 - 0.1 * (1.0 / (1.0 + 4.0 * pdist)));

		if (pdist<0.0) break;
	}
	
	return ao;
}

vec3 skyDome(vec3 rd)
{
	vec3 col = mix(vec3(0.01,0.025,0.1),vec3(0.1,0.25,0.4),clamp(pow(1.0-rd.y,4.0),0.0,1.0));
	col = mix(col,vec3(0.2,0.35,0.4),clamp(pow(1.0-rd.y,10.0),0.0,1.0));
	col = mix(col,vec3(0.3,0.27,0.25),clamp(pow(1.0-rd.y,20.0),0.0,1.0));

	return col;
}

// intersects the scene and returns a colour in rgb, distance along ray in a.
vec4 shadeScene(vec3 ro, vec3 rd)
{
	vec3 col = vec3(0.0);
	vec3 light_dir = normalize(vec3(0.3,0.5 + sin(iGlobalTime*0.2) * 0.3 ,0.7+ sin(iGlobalTime*0.3) * 0.1));

	vec2 scene_hit = intersectScene(ro,rd);
	
	if (scene_hit.y < 0.0)
	{ 
		scene_hit.x = MAXDIST;
		col = skyDome(rd);
	}
	else
	{
		vec3 pos = ro + rd * scene_hit.x;

		vec3 normal = deNormal(pos, rd);
		float light1 = queryLightSoft(pos + normal * 0.01, light_dir,0.0,MAXDIST,8.0);

		// Diffuse
		vec3 diffuse = vec3(0.9);//mix(vec3(1.0,0.5,0.0),vec3(0.5,0.0,0.8), scene_hit.y);
		col += diffuse * (clamp(dot(normal,light_dir),0.0,1.0)) * light1;

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
		col += vec3(0.1,0.25,0.4) * 0.5 * queryAO(pos , vec3(0.,1.,0.), 1.0);
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
		
		float dist_estimate = de(plane_p, rd).x;

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
	float fog = 1.0 / max(1.0,exp(dist * 0.01));
	
	if (dist<MAXDIST)	col = mix(vec3(0.3,0.27,0.25), col, fog);

	col = pow(col, vec3(1.0/2.2));  // gamma
	out_Col = vec4(col,1.0);
}




