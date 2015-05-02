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

in vec4 eyeTarget;

out vec4 out_Col;


float sdSphere(vec3 p, vec4 s)
{
	return length(s.xyz - p) - s.w;
}


float de(vec3 p)
{
	float s = 100000000.0;

	s = min(s,sdSphere(p,vec4(1.0,0.0,2.0,0.5)));

	s = min(s,sdSphere(p,vec4(1.0,0.0,0.0,1.0)));

	s = min(s,sdSphere(p,vec4(10.0,0.0,0.0,1.0)));

	return s;
}

float intersectPlane(vec3 ro, vec3 rd, vec4 plane)
{
	float d = dot(rd,plane.xyz);
	if (d==0.0) return -1.0;
	return -(dot(ro,plane.xyz)+plane.w) / d;
}


float isoLine(float x)
{
	return 1.0-smoothstep(0.04,0.05,abs(mod(x+0.5,1.0)-0.5));
}

void main() 
{

	vec3 ro = eyePos;
	vec3 rd = normalize(eyeTarget.xyz - eyePos);

	vec3 col = vec3(rd*0.5+0.5);


	// intersect with plane
	float plane_t = intersectPlane(ro,rd,vec4(0.0,1.0,0.0,0.0));

	if (plane_t > 0.0)
	{
		vec3 plane_p = ro + rd * plane_t;
		
		//col = vec3( (mod(plane_p.x,1.0) + mod(plane_p.z,1.0)) * 0.5 );
		float dist = de(plane_p);

		vec3 dcol1 = vec3(1.0,1.0,0.0);  // close
		vec3 dcol2 = vec3(1.0,0.0,0.0);  // medium
		vec3 dcol3 = vec3(0.0,0.0,1.0);  // far
		vec3 dcol4 = vec3(0.0,0.0,0.1);  // veryfar
		 
		col = mix(dcol1, dcol2, min(1.0,dist * 0.1));
		col = mix(col, dcol3, min(1.0,dist * 0.025));
		col = mix(col, dcol4, min(1.0,dist * 0.005));
		col *= 0.6;

		col += vec3(isoLine(dist*10.0)) * 0.1 / (1.0 + plane_t * 0.1);
		col += vec3(isoLine(dist)) * 0.1 / (1.0 + plane_t * 0.05);
		col += vec3(isoLine(dist * 0.1)) * 0.1 / (1.0 + plane_t * 0.005);
	}

	col = pow(col, vec3(1.0/2.2));  // gamma
	out_Col = vec4(col,1.0);
}




