#version 140

precision highp float;
 
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D detailTex;

uniform mat4 projection_matrix;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform vec4 boxparam;
uniform vec3 eyePos;
uniform float patchSize;
uniform float scale;
uniform float detailScale;
uniform vec2 offset;
uniform float detailTexScale; // 0.1

uniform vec4 detailWeights; // x:TL y:TR z:BL w:BR

in vec3 vertex;
in vec3 in_boxcoord;

out vec3 boxcoord;
out vec3 normal;
out vec3 binormal;
out vec3 tangent;

out vec2 detailpos_n;
out vec2 detailpos_s;
out vec2 detailpos_w;
out vec2 detailpos_e;

float t = 1.0 / boxparam.x;
float pt = t * detailScale;
float nx = 2.0 * detailScale;


float getHeightDetail(vec2 pos)
{
	return textureLod(detailTex,pos,0).r * detailTexScale;
}

float getHeight(vec2 pos,float weight)
{
	return textureLod(heightTex,pos,0).r;
}

// 4-tap b-spline bicubic interpolation.
// credit to http://vec3.ca/bicubic-filtering-in-fewer-taps/
float sampleHeight(vec2 pos, float weight)
{
	// get texel centre
	vec2 tc = pos * vec2(boxparam.x);
	vec2 itc = floor(tc);
	
	// fractional offset
	vec2 f = tc - itc;

	vec2 f2 = f*f;
	vec2 f3 = f2*f;

	// bspline weights
	vec2 w0 = f2 - (f3 + f) * 0.5;
    vec2 w1 = f3 * 1.5 - f2 * 2.5 + vec2(1.0);
    vec2 w3 = (f3 - f2) * 0.5;
    vec2 w2 = vec2(1.0) - w0 - w1 - w3;

	vec2 s0 = w0 + w1;
	vec2 s1 = w2 + w3;

	vec2 f0 = w1 / (w0 + w1);
	vec2 f1 = w3 / (w2 + w3);

	vec2 t0 = (itc - vec2(1.0) + f0) * t;
	vec2 t1 = (itc + vec2(1.0) + f1) * t;

	return 
		textureLod(heightTex,vec2(t0.x,t0.y),0).r * s0.x * s0.y +
		textureLod(heightTex,vec2(t1.x,t0.y),0).r * s1.x * s0.y +
		textureLod(heightTex,vec2(t0.x,t1.y),0).r * s0.x * s1.y +
		textureLod(heightTex,vec2(t1.x,t1.y),0).r * s1.x * s1.y;// + 
		//getHeightDetail(pos);
}

vec3 getNormal(vec2 pos, float weight)
{
	// get texel centre
	vec2 tc = pos * vec2(boxparam.x);
	vec2 itc = floor(tc);
	
	// fractional offset
	vec2 f = tc - itc;

	vec2 f2 = f*f;
	vec2 f3 = f2*f;

	// bspline weights
	vec2 w0 = f2 - (f3 + f) * 0.5;
    vec2 w1 = f3 * 1.5 - f2 * 2.5 + vec2(1.0);
    vec2 w3 = (f3 - f2) * 0.5;
    vec2 w2 = vec2(1.0) - w0 - w1 - w3;

	vec2 s0 = w0 + w1;
	vec2 s1 = w2 + w3;

	vec2 f0 = w1 / (w0 + w1);
	vec2 f1 = w3 / (w2 + w3);

	vec2 t0 = (itc - vec2(1.0) + f0) * t;
	vec2 t1 = (itc + vec2(1.0) + f1) * t;

	return normalize(
		(
			textureLod(normalTex,vec2(t0.x,t0.y),0).rgb * s0.x * s0.y +
			textureLod(normalTex,vec2(t1.x,t0.y),0).rgb * s1.x * s0.y +
			textureLod(normalTex,vec2(t0.x,t1.y),0).rgb * s0.x * s1.y +
			textureLod(normalTex,vec2(t1.x,t1.y),0).rgb * s1.x * s1.y
		) * 2.0 - vec3(1.0)
		);
}


// Displaces a flat mesh by the heightmap texture.
// Performs bicubic interpolation of heightmap heights and normals, then generates a 
// secondary detail displacement.
 
void main() {

	vec3 b = in_boxcoord;
	b.xz *= scale;
	b.xz += offset;

	vec2 texcoord = b.xz;

	float weight = 
		clamp(mix(
			mix(detailWeights.x,detailWeights.y,in_boxcoord.x),
			mix(detailWeights.z,detailWeights.w,in_boxcoord.x),
			in_boxcoord.z),0.0,1.0);

	highp vec2 pos = mod(texcoord,boxparam.x);

	float h = sampleHeight(pos,weight);
	normal = getNormal(pos,weight);

	// calculate tangent and binormal
	// tangent is in X direction, so is the cross product of normal (Y) and Z
	vec3 t1 = normalize(cross(normal,vec3(0.0,0.0,-1.0)));
	binormal = normalize(cross(t1,normal));
	tangent = normalize(cross(normal,binormal));

	vec2 detailpos = pos * 32.0;
	float dt = 1.0/1024.0;
	detailpos_n = detailpos - vec2(0.0,dt);
	detailpos_s = detailpos + vec2(0.0,dt);
	detailpos_w = detailpos - vec2(dt,0.0);
	detailpos_e = detailpos + vec2(dt,0.0);

	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h + v.y * 1.0;

	v += normal * getHeightDetail(detailpos);

    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(v, 1.0);

	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;
	//worldpos = (model_matrix * vec4(b,1.0)).xyz - eyePos;

}
