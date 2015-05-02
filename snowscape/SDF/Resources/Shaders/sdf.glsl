﻿//|vs
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

// returns the parameter with the smallest x component
vec2 dunion(vec2 a, vec2 b)
{
	return a.x<b.x?a:b;
}

// creates an object from an id and distance
vec2 ob(float hitid, float d)
{
	return vec2(d,hitid);
}

// modulus on x axis, returns instance id
float pmodx(inout vec3 p, float m)
{
	p.x = (mod((p.x / m) + 0.5, 1.0) - 0.5) * m;

	return 0.0;
}

// translation
vec3 tr(vec3 p, vec3 t) { return p-t; }
vec3 tr_x(vec3 p, float t) { return tr(p,vec3(t,0.,0.)); }
vec3 tr_y(vec3 p, float t) { return tr(p,vec3(0.,t,0.)); }
vec3 tr_z(vec3 p, float t) { return tr(p,vec3(0.,0.,t)); }


// distance estimator
// returns distance bound in x, hit id in y
vec2 de(vec3 p)
{
	vec2 s = vec2(100000000.0,-1);

	//pmodx(p,5.0);

	s = dunion(s,ob(1.0,sdSphere(tr_x(p,1.0),0.5)));
	s = dunion(s,ob(1.0,sdSphere(tr_x(p,2.0),0.75)));
	s = dunion(s,ob(1.0,sdSphere(tr_x(p,3.0),1.0)));

	s = dunion(s,ob(0.5,sdBox(tr_x(p,-3.0),vec3(1.,2.,3.))));
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

const float MAXDIST = 100000000.0;

// raymarches the scene described by distance estimator de()
// returns distance in x, hit id in y
vec2 intersectScene(vec3 ro, vec3 rd)
{
	int i=0;
	float t=0.0;
	vec2 pdist = vec2(MAXDIST,-1.0);
	vec2 res = pdist;
	float ssign = de(ro).x < 0.0 ? -1.0 : 1.0;

	if (ssign < 0.0) return res;

	while (i<100 && pdist.x > 0.0001)
	{
		vec3 p = ro + rd * t;  // position along ray
		pdist = de(p); // get distance bound

		t += pdist.x; // move position
		i++;
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

	return vec4(normal * 0.5+0.5, scene_hit.x);
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
	

	// intersect with plane
	float plane_t = intersectPlane(ro,rd,vec4(0.0,1.0,0.0,0.0));

	if (plane_t > 0.0 && plane_t < scene_col.a)
	{
		vec3 plane_p = ro + rd * plane_t;
		
		//col = vec3( (mod(plane_p.x,1.0) + mod(plane_p.z,1.0)) * 0.5 );
		float dist = de(plane_p).x;

		vec3 dcol1 = vec3(1.0,0.5,0.0);  // close
		vec3 dcol2 = vec3(1.0,0.02,0.05);  // medium
		vec3 dcol3 = vec3(0.1,0.02,0.2);  // far
		//vec3 dcol4 = vec3(0.0,0.0,0.1);  // veryfar
		 
		float adist = sqrt(abs(dist));
		col = mix(dcol1, dcol2, clamp(adist,0.0,1.0));
		col = mix(col, dcol3, clamp((adist-1.0) * 0.5,0.0,1.0));
		//col = mix(col, dcol3, min(1.0,(dist-10.0) * 0.025));
		//col = mix(col, dcol4, min(1.0,dist * 0.005));

		if (dist < 0.0) col = vec3(1.0) - col;

		col *= 0.6;
		
		float isoline_intensity = 0.2 / (1.0 + plane_t*0.2);
		// distance field isolines
		float distance_isoline = max(isoLine(dist * 10.0,0.05),isoLine(dist,0.02));
		col += vec3(1.0,0.5,0.1) * (distance_isoline * isoline_intensity);

		// xz grid isolines
		col += vec3(0.2,1.0,0.5) * (isoLine(plane_p.x,0.01) * isoline_intensity);
		col += vec3(0.2,0.5,1.0) * (isoLine(plane_p.z,0.01) * isoline_intensity);

		//col += vec3(isoLine(dist * 100.0,0.1) * (0.1 / (1.0 + plane_t)));
		//col += vec3(isoLine(dist * 10.0,0.05) * (0.1 / (1.0 + plane_t * 0.1)));
		//col += vec3(isoLine(dist,0.02) * (0.1 / (1.0 + plane_t * 0.05)));
		
		// xz isolines
		//col += vec3(isoLine(plane_p.x,0.01) * 0.1);

	}

	col = pow(col, vec3(1.0/2.2));  // gamma
	out_Col = vec4(col,1.0);
}




