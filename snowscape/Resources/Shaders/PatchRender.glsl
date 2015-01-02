//|CubicHeightSample
// 4-tap b-spline bicubic interpolation.
// credit to http://vec3.ca/bicubic-filtering-in-fewer-taps/
float sampleHeight(vec2 pos, float scale)
{
	// get texel centre
	vec2 tc = pos * vec2(scale);
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
		textureLod(heightTex,vec2(t1.x,t1.y),0).r * s1.x * s1.y;
}

//|CubicNormalSample
vec3 getNormal(vec2 pos, float scale)
{
	// get texel centre
	vec2 tc = pos * vec2(scale);
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
		) 
		);
}



//|LowVertex
#version 140
 
uniform sampler2D heightTex;

uniform mat4 transform_matrix;
uniform vec4 boxparam;
uniform vec3 eyePos;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;

in vec3 vertex;
in vec3 in_boxcoord;

out vec3 boxcoord;
out vec2 texcoord;

float t = 1.0 / boxparam.x;

float getHeight(vec2 pos)
{
	return textureLod(heightTex,pos,0).r;
}

float sampleHeight(vec2 pos)
{
	float c = getHeight(pos);
	return c;
}

void main() {

	vec3 b = in_boxcoord;
	b.xz *= scale;
	b.xz += offset;
	texcoord = b.xz;

	float h = sampleHeight(texcoord);

	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h + v.y;

    gl_Position = transform_matrix * vec4(v, 1.0);

	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;

}

//|LowFragment

#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D paramTex;
uniform sampler2D shadeTex;
uniform vec4 boxparam;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailTexScale; 

in vec3 boxcoord;
in vec2 texcoord;

out vec4 out_Colour;
out vec4 out_Normal;
out vec4 out_Shading;
out vec4 out_Lighting;


void main(void)
{
	vec2 shadowAO = texture(shadeTex,texcoord).rg;
	float height = textureLod(heightTex,texcoord,0).r;

	out_Colour = vec4(0.2,0.2,0.6,0.1);
    out_Normal = texture(normalTex,texcoord);

	float shadow = smoothstep(-1.0,-0.02,height - shadowAO.r);

	out_Shading = vec4(0.0);
	out_Lighting = vec4(shadow,shadowAO.g,0.0,0.0);

}

//|MediumVertex
#version 140
 
uniform sampler2D heightTex;

uniform mat4 transform_matrix;
uniform vec4 boxparam;
uniform vec3 eyePos;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;

in vec3 vertex;
in vec3 in_boxcoord;

out vec3 boxcoord;
out vec2 texcoord;

float t = 1.0 / boxparam.x;

float getHeight(vec2 pos)
{
	return textureLod(heightTex,pos,0).r;
}

float sampleHeight(vec2 pos)
{
	float c = getHeight(pos);
	return c;
}

void main() {

	vec3 b = in_boxcoord;
	b.xz *= scale;
	b.xz += offset;
	texcoord = b.xz;

	float h = sampleHeight(texcoord);

	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h + v.y;

    gl_Position = transform_matrix * vec4(v, 1.0);

	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;

}

//|MediumFragment

#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D paramTex;
uniform sampler2D shadeTex;
uniform vec4 boxparam;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailTexScale; 

in vec3 boxcoord;
in vec2 texcoord;

out vec4 out_Colour;
out vec4 out_Normal;
out vec4 out_Shading;
out vec4 out_Lighting;


void main(void)
{
	vec2 shadowAO = texture(shadeTex,texcoord).rg;
	float height = textureLod(heightTex,texcoord,0).r;

	out_Colour = vec4(0.2,0.6,0.2,0.1);
    out_Normal = texture(normalTex,texcoord);

	float shadow = smoothstep(-1.0,-0.02,height - shadowAO.r);

	out_Shading = vec4(0.0);
	out_Lighting = vec4(shadow,shadowAO.g,0.0,0.0);

}


//|HighVertex
#version 140
 
uniform sampler2D heightTex;
uniform sampler2D normalTex;

uniform mat4 transform_matrix;
uniform vec4 boxparam;
uniform vec3 eyePos;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;

in vec3 vertex;
in vec3 in_boxcoord;

out vec3 boxcoord;
out vec3 normal;
out vec3 binormal;
out vec3 tangent;
out vec2 texcoord;


float t = 1.0 / boxparam.x;

float getHeight(vec2 pos)
{
	return textureLod(heightTex,pos,0).r;
}

float sampleHeight(vec2 pos)
{
	float c = getHeight(pos);
	return c;
}

#include ".|CubicHeightSample"

#include ".|CubicNormalSample"

void main() {

	vec3 b = in_boxcoord;
	b.xz *= scale;
	b.xz += offset;
	texcoord = b.xz;

	float h = sampleHeight(texcoord,boxparam.x);
	normal = getNormal(texcoord,boxparam.x);

	// calculate tangent and binormal
	// tangent is in X direction, so is the cross product of normal (Y) and Z
	vec3 t1 = normalize(cross(normal,vec3(0.0,0.0,-1.0)));
	binormal = normalize(cross(t1,normal));
	tangent = normalize(cross(normal,binormal));


	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h + v.y;

    gl_Position = transform_matrix * vec4(v, 1.0);

	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;

}

//|HighFragment

#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D paramTex;
uniform sampler2D shadeTex;
uniform vec4 boxparam;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailTexScale; 

in vec3 boxcoord;
in vec3 normal;
in vec3 binormal;
in vec3 tangent;
in vec2 texcoord;

out vec4 out_Colour;
out vec4 out_Normal;
out vec4 out_Shading;
out vec4 out_Lighting;


void main(void)
{
	vec2 shadowAO = texture(shadeTex,texcoord).rg;

	float height = boxcoord.y;//textureLod(heightTex,texcoord,0).r;

	out_Colour = vec4(0.6,0.2,0.2,0.1);
    out_Normal = vec4(normalize(normal),1.0);

	float shadow = smoothstep(-1.0,-0.02,height - shadowAO.r);

	out_Shading = vec4(0.0);
	out_Lighting = vec4(shadow,shadowAO.g,0.0,0.0);

}


