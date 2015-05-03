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
vec2 de(vec3 p)
{
	vec2 s = vec2(100000000.0,-1);

	//float m = 10.0;
	//pmod2(p.xz,vec2(m));

	s = dunion(s,de_boxcyl(p));
	//s = dunion(s,de_boxcyl(p - vec3(m,0.0,0.0)));
	//s = dunion(s,de_boxcyl(p - vec3(0.0,0.0,m)));

	//s = dunion(s,ob(1.0,sdSphere(tr_x(p,1.0),0.5)));
	//s = dunion(s,ob(1.0,sdSphere(tr_x(p,2.0),0.75)));
	//s = dunion(s,ob(1.0,sdSphere(tr_x(p,3.0),1.0)));

	//s = dunion(s,ob(0.5,sdBox(tr_x(p,-1.5),vec3(1.,2.,3.))));
	//float cyl = sdCylinderz(p,0.5,1.0);
	//s = dcombine( ob(0.5,sdBox(tr_x(p,-1.5),vec3(1.,2.,3.))) ,ob(1.0,cyl),1.0);

	return s;
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

	// check to see if we're inside the scene
	// if we are, find the boundary (rough) and set ray origin to there
	//float ndist = de(ro).x;
	//while (ndist < 0.0)
	//{
	//	ro -= rd * ndist;
	//	ndist = de(ro).x;
	//}
	if (de(ro).x < 0.0) return res;

	for(int i=0;i<150.0;i++)
	{
		vec3 p = ro + rd * t;  // position along ray
		pdist = de(p); // get distance bound

		t += pdist.x; // move position

		if (pdist.x < 0.00005)
		{
			break;
		}

		if (t > MAXDIST)
		{
			pdist.y = -1.0;
			break;
		}

	}
	res.x = t;
	res.y = pdist.y;

	return res;
}

// intersects the scene and returns a colour in rgb, distance along ray in a.
vec4 shadeScene(vec3 ro, vec3 rd)
{
	vec2 scene_hit = intersectScene(ro,rd);
	
	if (scene_hit.y < 0.0) return vec4(0.0,0.0,0.0,MAXDIST); // no hit

	vec3 normal = deNormal(ro + rd * scene_hit.x, rd);

	vec3 diffuse = mix(vec3(1.0,0.5,0.0),vec3(0.5,0.0,0.8), scene_hit.y);
	vec3 light_dir = normalize(vec3(0.3,0.8,0.7));
	vec3 col = diffuse * (clamp(dot(normal,light_dir),-1.0,1.0) * 0.5 + 0.5);

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
	float plane_t = intersectPlane(ro,rd,vec4(0.0,1.0,0.0,-wheel));

	if (plane_t > 0.0 && plane_t < scene_col.a)
	{
		vec3 plane_p = ro + rd * plane_t;
		
		float dist_estimate = de(plane_p).x;

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
	float fog = 1.0 / max(1.0,exp(dist * 0.005));
	col *= fog;

	col = pow(col, vec3(1.0/2.2));  // gamma
	out_Col = vec4(col,1.0);
}




