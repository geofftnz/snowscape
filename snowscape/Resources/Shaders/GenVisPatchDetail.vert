#version 140
 
uniform sampler2D heightTex;

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
	//return 0.0;
	return noise(vec3(pos * 4096.0,1.0)) * 0.1;
}

float getHeight(vec2 pos,float weight)
{
	return texture(heightTex,pos).r;
}

// finite difference
float sampleHeight(vec2 pos, float weight)
{
	float c = getHeight(pos,weight);
	float n = getHeight(vec2(pos.x, pos.y - t),weight);
	float s = getHeight(vec2(pos.x, pos.y + t),weight);
	float w = getHeight(vec2(pos.x - t, pos.y),weight);
	float e = getHeight(vec2(pos.x + t, pos.y),weight);
	return ((c * 2.0 + n+s+w+e) / 6.0) + getHeightDetail(pos) * weight;
}

vec3 getNormal(vec2 pos, float weight)
{

    float h1 = sampleHeight(vec2(pos.x, pos.y - t),  weight);
	float h2 = sampleHeight(vec2(pos.x, pos.y + t),  weight);
    float h3 = sampleHeight(vec2(pos.x - t, pos.y),  weight);
	float h4 = sampleHeight(vec2(pos.x + t, pos.y),  weight);

    //return normalize(vec3(h4-h3,h2-h1,1.0));
	return normalize(vec3(h3-h4,2.0,h1-h2));
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

	float h = sampleHeight(texcoord,weight);
	normal = getNormal(texcoord,weight);

	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h + v.y * (boxparam.w - boxparam.z) * 0.005;

    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(v, 1.0);

	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;
	worldpos = (model_matrix * vec4(b,1.0)).xyz - eyePos;

}
