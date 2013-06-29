#version 140
 
uniform sampler2D heightTex;

uniform mat4 projection_matrix;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform vec4 boxparam;
uniform vec3 eyePos;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;

in vec3 vertex;
in vec3 in_boxcoord;

out vec3 boxcoord;
out vec3 worldpos;
out vec3 normal;

float t = 1.0 / boxparam.x;



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




float getHeight(vec2 pos)
{
	return texture(heightTex,pos).r + noise(vec3(pos * 8192.0,1.0)) * 0.1;
}

// finite difference
float sampleHeight(vec2 pos)
{
	float c = getHeight(pos);
	float n = getHeight(vec2(pos.x, pos.y - t));
	float s = getHeight(vec2(pos.x, pos.y + t));
	float w = getHeight(vec2(pos.x - t, pos.y));
	float e = getHeight(vec2(pos.x + t, pos.y));
	return (c * 4.0 + n+s+w+e) / 8.0;
}

vec3 getNormal(vec2 pos)
{

    float h1 = sampleHeight(vec2(pos.x, pos.y - t));
	float h2 = sampleHeight(vec2(pos.x, pos.y + t));
    float h3 = sampleHeight(vec2(pos.x - t, pos.y));
	float h4 = sampleHeight(vec2(pos.x + t, pos.y));

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



	float h = sampleHeight(texcoord);
	normal = getNormal(texcoord);

	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h;

    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(v, 1.0);

	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;
	worldpos = (model_matrix * vec4(b,1.0)).xyz - eyePos;

}
