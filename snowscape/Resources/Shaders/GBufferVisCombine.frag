#version 140
precision highp float;

uniform sampler2D posTex;
//uniform sampler2D normalTex;
uniform sampler2D paramTex;
uniform sampler2D heightTex;
uniform sampler2D shadeTex;

uniform vec4 boxparam;
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

float texel = 1.0 / boxparam.x;

float sampleHeight(vec2 posTile)
{
	return texture2D(heightTex,posTile * texel).r;
}

float sampleHeightNoise(vec2 posTile, float f, float a)
{
	return sampleHeight(posTile) + fbm(vec3(posTile.xy*f,0.0)) * a;
}

// pos in tile coords (0-boxparam.xy)
vec3 getNormal(vec2 pos)
{
    float h1 = sampleHeight(vec2(pos.x, pos.y - 0.5));
	float h2 = sampleHeight(vec2(pos.x, pos.y + 0.5));
    float h3 = sampleHeight(vec2(pos.x - 0.5, pos.y));
	float h4 = sampleHeight(vec2(pos.x + 0.5, pos.y));

    return normalize(vec3(h3-h4,1.0,h1-h2));
}

vec3 getNormalNoise(vec2 pos, float f, float a)
{
    float h1 = sampleHeightNoise(vec2(pos.x, pos.y - 0.5),f,a);
	float h2 = sampleHeightNoise(vec2(pos.x, pos.y + 0.5),f,a);
    float h3 = sampleHeightNoise(vec2(pos.x - 0.5, pos.y),f,a);
	float h4 = sampleHeightNoise(vec2(pos.x + 0.5, pos.y),f,a);

    return normalize(vec3(h3-h4,1.0,h1-h2));
}

vec3 getSkyColour(vec3 skyvector)
{
	vec3 skycol = 
		mix(
			vec3(0.02,0.03,0.2),
			vec3(0.4,0.6,0.9),
			pow(clamp(1.0-dot(skyvector,vec3(0.0,1.0,0.0)),0.0,1.0),2.0)
			);

	// scattering around the sun
	skycol += vec3(1.0,0.9,0.3) * pow(clamp(dot(skyvector,sunVector),0.0,1.0),300.0) * 4.0;

	// sun disk
	skycol += vec3(1.0,0.9,0.6) * smoothstep(0.9998,0.99995,dot(skyvector,sunVector)) * 8.0;

	return skycol;
}

float directIllumination(vec3 p, vec3 n, float shadowHeight)
{
	return smoothstep(-2.0,-0.1,p.y - shadowHeight) * clamp(dot(n,sunVector)*0.5+0.5,0,1);
}


vec4 generateCol(vec3 p, vec3 n, vec4 s, float shadowHeight, float AO)
{
	vec4 colH1 = pow(vec4(0.3,0.247,0.223,1.0),vec4(2.0));
	vec4 colL1 = pow(vec4(0.41,0.39,0.16,1.0),vec4(2.0));

	vec4 colW = pow(vec4(0.7,0.8,1.0,1.0),vec4(2.0));

	float looseblend = s.r*s.r;

	vec4 col = mix(colH1,colL1,looseblend);
    //col *= 1.3;


	vec3 eyeDir = normalize(p-eyePos);
	vec3 wCol = vec3(0.1,0.2,0.25) + getSkyColour(reflect(eyeDir,n)) * smoothstep(-2.0,-0.1,p.y - shadowHeight);

	vec4 colW0 = vec4(wCol,1.0);  // blue water
	//vec4 colW0 = vec4(0.4,0.7,0.95,1.0);  // blue water
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

    //vec3 l = normalize(vec3(0.4,0.6,0.2));

	float diffuse = directIllumination(p,n,shadowHeight);
	//float diffuse = clamp(dot(n,sunVector) * 0.5 + 0.5,0,1);
	//float diffuse = clamp(dot(n,sunVector),0,1);
	
	//col *= diffuse + 0.05;  //ambient

	col = col * diffuse + col * vec4(0.8,0.9,1.0,1.0) * 0.7 * AO;

	return col;
}



void main(void)
{
	vec4 c = vec4(0.0,0.0,0.0,1.0);
	
	vec2 p = texcoord0.xy;
	vec4 posT = texture2D(posTex,p);
	float hitType = posT.a;
	vec4 pos = vec4(posT.xyz + eyePos,0.0);
	//vec4 normalT = texture2D(normalTex,p);
	vec4 paramT = texture2D(paramTex,p);
	//vec3 normal = normalize(normalT.xyz - 0.5);

	vec3 wpos = pos.xyz - eyePos;

	float smoothness = smoothstep(0.02,0.1,paramT.g)*8.0 + paramT.r*paramT.r * 2.0;
	
	vec3 normal = getNormalNoise(pos.xz,0.76,1.0 / (1.0+smoothness));
	//vec3 normal = getNormal(pos.xz);

	vec2 shadowAO = texture2D(shadeTex,pos.xz * texel).rg;

	float d = length(wpos);

	if (hitType > 0.6)
	{

	
		c = generateCol(pos.xyz,normal,paramT, shadowAO.r, shadowAO.g);	

		vec4 fogcol = vec4(0.6,0.8,1.0,1.0);
		d /= 1024.0;
		float fogamount = 1.0 / (exp(d * d * 0.2));

		if (hitType < 0.5){
			fogamount = 0.0;
		}

		c = mix(fogcol,c,fogamount);
		
		//c.r = shadowAO.r;
		//c.g = shadowAO.g;
		//c.rgb = vec3(shadowAO.g);

		// visualize normal
		//c = vec4(normal*0.5+0.5,1.0);

		// visualize eye direction vector
		//c = vec4(normalize(pos.xyz - eyePos)*0.5+0.5,1.0);
	}
	else
	{
		if (hitType > 0.05)
		{

			//vec3 l = normalize(vec3(0.4,0.6,0.2));
			
			vec3 skycol = getSkyColour(normalize(posT.xyz));
			c = vec4(skycol,1.0);
			
			//vec4 skycol = mix(vec4(0.6,0.8,1.0,1.0),vec4(0.1,0.1,0.4,1.0),clamp(dot(posT.xyz,vec3(0.0,-1.0,0.0)),0.0,1.0));
			//c = mix(skycol,vec4(1.0),pow(clamp(dot(posT.xyz,-sunVector),0.0,1.0),50.0));

			// visualize eye direction vector
			//c = vec4(posT.xyz*0.5+0.5,1.0);
		}
		else
		{
			c = vec4(1.0,1.0,0.0,1.0);
		}
	}



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

	//out_Colour = vec4(c.rgb,1.0);
    out_Colour = vec4(sqrt(c.rgb),1.0);
	//out_Colour = vec4(pow(c.rgb,vec3(0.45)),1.0);
}
