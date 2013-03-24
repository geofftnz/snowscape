#version 140
precision highp float;

uniform sampler2D posTex;
uniform sampler2D normalTex;
uniform sampler2D paramTex;

uniform vec3 eyePos;
uniform vec3 sunVector;

in vec2 texcoord0;
out vec4 out_Colour;

mat3 m = mat3( 0.00,  0.80,  0.60,
              -0.80,  0.36, -0.48,
              -0.60, -0.48,  0.64 );

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
    f  = 0.5000*noise( p ); p = m*p*2.02;
    f += 0.2500*noise( p ); p = m*p*2.03;
    f += 0.1250*noise( p ); p = m*p*2.01;
    f += 0.0625*noise( p );
    return f;
}


vec4 generateCol(vec3 p, vec3 n, vec4 s)
{
	vec4 colH1 = vec4(0.3,0.247,0.223,1.0);
	vec4 colH2 = vec4(0.3,0.247,0.223,1.0);

	vec4 colL1 = vec4(0.41,0.39,0.16,1.0);
	vec4 colL2 = vec4(0.41,0.39,0.16,1.0);

	vec4 colW = vec4(0.7,0.8,1.0,1.0);

	float h = 0.0;//p.y / 1024.0;

	float looseblend = s.r*s.r;

	vec4 col = mix(mix(colH1,colH2,h),mix(colL1,colL2,h),looseblend);
    col *= 1.4;

	vec4 colW0 = vec4(0.4,0.7,0.95,1.0);  // blue water
	vec4 colW1 = vec4(0.659,0.533,0.373,1.0);  // dirty water
	vec4 colW2 = vec4(1.2,1.3,1.4,1.0); // white water

	colW = mix(colW0,colW1,clamp(s.b*1.5,0,1));  // make water dirty->clean

	//colW = mix(colW,colW2,smoothstep(0.05,0.8,s.a)*0.8);  // speed -> white water

	//col = mix(col,colW,clamp(s.g*s.g*16.0,0,0.6)); // water
	float waterblend = smoothstep(0.02,0.1,s.g) * 0.1 + 0.4 * s.g * s.g;

	col = mix(col,colW,waterblend); // water

    // misc vis
	vec4 colE = vec4(0.4,0.6,0.9,1.0);
	col += colE * clamp(s.a,0.0,1.0);

    vec3 l = normalize(vec3(0.4,0.6,0.2));

	float diffuse = clamp(dot(n,l) * 0.5 + 0.5,0,1);
	col *= (0.4 + 0.6 * diffuse);

	return col;
}


void main(void)
{
	vec4 c = vec4(0.0,0.0,0.0,1.0);
	
	vec2 p = texcoord0.xy;
	vec4 posT = texture2D(posTex,p);
	float hitType = posT.a;
	vec4 pos = vec4(posT.xyz + eyePos,0.0);
	vec4 normalT = texture2D(normalTex,p);
	vec4 paramT = texture2D(paramTex,p);
	vec3 normal = normalize(normalT.xyz - 0.5);

	float d = length(pos.xyz - eyePos);

	c = generateCol(pos,normal,paramT);	
	
	vec4 fogcol = vec4(0.8, 0.88, 0.92,1.0);
	d /= 1024.0;
	float fogamount = 1.0 / (exp(d * d * 0.1));

	if (hitType < 0.5){
		fogamount = 0.0;
	}

	c = mix(fogcol,c,fogamount);

	/*
	vec2 p = texcoord0.xy * 2.0;
	// split screen into 4
	if (p.x < 1.0)
	{
		if (p.y < 1.0)
		{
			vec3 pos = texture2D(posTex,p).xyz + eyePos;
			c.rgb = pos.xyz / 1024.0;
		}
		else
		{
			c = texture2D(normalTex,p-vec2(0.0,1.0));
		}
	}
	else
	{
		if (p.y < 1.0)
		{
			c = vec4(0.0);
		}
		else
		{
			c = texture2D(paramTex,p-vec2(1.0,1.0));
		}
	}
	*/

	// fog


    out_Colour = vec4(c.rgb,1.0);
}
