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

uniform vec4 detailWeights; // x:TL y:TR z:BL w:BR

in vec3 vertex;
in vec3 in_boxcoord;

out vec3 boxcoord;
out vec3 worldpos;
out vec3 normal;

float t = 1.0 / boxparam.x;
float pt = t * detailScale;
float nx = 2.0 * detailScale;



mat3 m = mat3( 0.00,  0.80,  0.60,
              -0.80,  0.36, -0.48,
              -0.60, -0.48,  0.64 );

float rand(vec2 co){
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

float rand(vec3 co){
    return fract(sin(dot(co.xyz ,vec3(12.9898,78.233,47.985))) * 43758.5453);
}

// credit: iq/rgba
float hash( float n )
{
    return fract(sin(n)*43758.5453);
}


// credit: iq/rgba
float noise( in vec3 x )
{
    vec3 p = floor(x);
    vec3 f = fract(x);
    f = f*f*(3.0-2.0*f);
    float n = p.x + p.y*57.0 + 113.0*p.z;
    float res = mix(mix(mix( hash(n+  0.0), hash(n+  1.0),f.x),
                        mix( hash(n+ 57.0), hash(n+ 58.0),f.x),f.y),
                    mix(mix( hash(n+113.0), hash(n+114.0),f.x),
                        mix( hash(n+170.0), hash(n+171.0),f.x),f.y),f.z);
    return res;
}


// credit: iq/rgba
float fbm( vec3 p )
{
    float f;
    f  = 0.5000*noise( p );
    p = m*p*2.02;
    f += 0.2500*noise( p );
    p = m*p*2.03;
    f += 0.1250*noise( p );
    p = m*p*2.01;
    f += 0.0625*noise( p );
    return f;
}



float getHeightDetail(vec2 pos)
{
	return 0.0;
	//return noise(vec3(pos * 4096.0,1.0)) * 0.03 + noise(vec3(pos * 8354.0,17.0)) * 0.015 + noise(vec3(pos * 17354.0,189.0)) * 0.007;

	//return textureLod(detailTex,pos * 32.0 ,0).r * 0.1;
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
		textureLod(heightTex,vec2(t1.x,t1.y),0).r * s1.x * s1.y + 
		getHeightDetail(pos);
}

//vec3 getNormal(vec2 pos, float weight)
//{
	//float tt = t;
	//float w = 2.0;
//
    //float h1 = sampleHeight(vec2(pos.x, pos.y - tt),  weight);
	//float h2 = sampleHeight(vec2(pos.x, pos.y + tt),  weight);
    //float h3 = sampleHeight(vec2(pos.x - tt, pos.y),  weight);
	//float h4 = sampleHeight(vec2(pos.x + tt, pos.y),  weight);
//
    ////return normalize(vec3(h4-h3,h2-h1,1.0));
	//return normalize(vec3(h3-h4,w,h1-h2));
//}

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


/*
float texel = 1.0 / boxparam.x;
float sampleHeight(vec2 posTile)
{
    return texture(heightTex,posTile * texel).r;
}


// pos in tile coords (0-boxparam.xy)
vec3 getNormal(vec2 pos)
{
	//pos *= boxparam.x; 
    float h1 = sampleHeight(vec2(pos.x, pos.y - 1.0));
    float h2 = sampleHeight(vec2(pos.x, pos.y + 1.0));
    float h3 = sampleHeight(vec2(pos.x - 1.0, pos.y));
    float h4 = sampleHeight(vec2(pos.x + 1.0, pos.y));
    return normalize(vec3(h3-h4,2.0,h1-h2));
}*/

 
void main() {

	//vec2 texcoord = in_boxcoord.xz;

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

	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h + v.y * 0.2;

    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(v, 1.0);

	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;
	worldpos = (model_matrix * vec4(b,1.0)).xyz - eyePos;

}
