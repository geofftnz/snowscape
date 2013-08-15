#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D paramTex;
uniform sampler2D detailTex;
uniform vec4 boxparam;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailScale;

in vec3 boxcoord;
in vec3 worldpos;
in vec3 normal;
in vec3 binormal;
in vec3 tangent;
in vec2 detailpos;


out vec4 out_Pos;
out vec4 out_Normal;
out vec4 out_Param;


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


float t = 1.0 / 1024.0;
float sampleHeight(vec2 pos)
{
	return textureLod(detailTex,pos,0).r * 0.25;
}

vec3 getDetailNormal(vec2 pos)
{
	//pos *= 32.0;
	//vec3 ofs = vec3(-t,0.0,t);

	float w = 2.0 / 4.0;

    //float h1 = sampleHeight(pos + ofs.yx); // 0,-1
    //float h2 = sampleHeight(pos + ofs.yz);  // 0 1
    //float h3 = sampleHeight(pos + ofs.xy); // -1 0
    //float h4 = sampleHeight(pos + ofs.zy); // 1 0
    float h1 = sampleHeight(vec2(pos.x, pos.y - t));
    float h2 = sampleHeight(vec2(pos.x, pos.y + t));
    float h3 = sampleHeight(vec2(pos.x - t, pos.y));
    float h4 = sampleHeight(vec2(pos.x + t, pos.y));
    return normalize(vec3(h4-h3,w,h2-h1));  // WAT
}



void main(void)
{
	vec2 texcoord = boxcoord.xz/boxparam.xy;
    //float h = texture2D(heightTex,texcoord).r;

	// calculate normal of detail heightmap at detailpos

	mat3 nm = mat3(tangent,normal,binormal);

	//vec3 dn = vec3(0.0,1.0,0.0);
	vec3 dn = getDetailNormal(detailpos);

	//vec3 n = getDetailNormal(detailpos);
	vec3 n = normalize(dn * nm);
	//vec3 n = normal;

    //vec3 n = normal; //normalize(normal);
	/*
	vec3 npos = worldpos.xyz * 64.0;
	vec3 n2 = 
		vec3(
			fbm(npos),
			fbm(npos + vec3(3.7,7.8,9.0)),
			fbm(npos + vec3(9.7,3.8,19.0))
		) - vec3(0.5);

	n = normalize(n + n2 * 0.1);*/

    out_Pos = vec4(worldpos.xyz,1.0);
    out_Normal = vec4(n.xyz * 0.5 + 0.5,1.0);
	out_Param = texture(paramTex,texcoord);

}
