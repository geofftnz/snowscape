#version 140
precision highp float;

uniform sampler2D heightTex;
uniform sampler2D shadeTex;

in vec2 texcoord0;
out vec4 out_Colour;

const float TEXEL = 1.0 / 1024.0;
const float NTEXEL = 0.5 / 1024.0;
const float SMTEXEL = 0.25 / 1024.0;

float sampleHeight(vec2 p)
{
    vec2 p0 = trunc(p * 1024.0) /1024.0;
    vec2 pf = clamp((p-p0) * 1024.0,0.0,1.0);

    float h00 = texture2D(heightTex,vec2(p0.x,p0.y)).r;
    float h01 = texture2D(heightTex,vec2(p0.x,p0.y+TEXEL)).r;
    float h10 = texture2D(heightTex,vec2(p0.x+TEXEL,p0.y)).r;
    float h11 = texture2D(heightTex,vec2(p0.x+TEXEL,p0.y+TEXEL)).r;

    return mix(mix(h00,h10,pf.x),mix(h01,h11,pf.x),pf.y);
}

vec3 getNormal(vec2 pos)
{
    float h1 = sampleHeight(vec2(pos.x,pos.y-NTEXEL)).r;
    float h2 = sampleHeight(vec2(pos.x,pos.y+NTEXEL)).r;
    float h3 = sampleHeight(vec2(pos.x-NTEXEL,pos.y)).r;
    float h4 = sampleHeight(vec2(pos.x+NTEXEL,pos.y)).r;

    return normalize(vec3(h4-h3,h2-h1,2.0*NTEXEL));
}

float contour(float h0, float h1,float h2, float h3, float h4, float contourscale)
{
	float b0 = floor(h0*contourscale);
	float b1 = floor(h1*contourscale);
	float b2 = floor(h2*contourscale);
	float b3 = floor(h3*contourscale);
	float b4 = floor(h4*contourscale);

	return (b0>b1 || b0>b2 || b0>b3 || b0>b4) ? 1.0:0.0;
}


void main(void)
{

	vec4 colH1 = vec4(0.3,0.247,0.223,1.0);
	vec4 colH2 = vec4(0.3,0.247,0.223,1.0);

	vec4 colL1 = vec4(0.41,0.39,0.16,1.0);
	vec4 colL2 = vec4(0.41,0.39,0.16,1.0);

	vec4 colW = vec4(0.7,0.8,1.0,1.0);

	vec4 s = texture2D(shadeTex,texcoord0.st);
	float h = sampleHeight(texcoord0.st);

	float looseblend = clamp(s.r * s.r * 2.0,0.0,1.0);
	vec4 col = mix(mix(colH1,colH2,h),mix(colL1,colL2,h),looseblend);
    col *= 1.4;

	vec4 colW0 = vec4(0.4,0.7,0.95,1.0);  // blue water
	vec4 colW1 = vec4(0.659,0.533,0.373,1.0);  // dirty water
	vec4 colW2 = vec4(1.4,1.4,1.4,1.0); // white water

	colW = mix(colW0,colW1,clamp(s.b*1.5,0,1));  // make water dirty->clean
	colW = mix(colW,colW2,s.a*0.8);  // speed -> white water

	col = mix(col,colW,clamp(s.g*s.g*16.0,0,0.6)); // water

    // misc vis
	//vec4 colE = vec4(1.0,0.0,1.0,1.0);
	//col += colE * clamp(s.a,0.0,1.0);


    vec3 n = getNormal(texcoord0.st);
    vec3 l = normalize(vec3(0.4,0.6,0.2));

	float diffuse = clamp(dot(n,l) * 0.5 + 0.5,0,1);
	col *= (0.4 + 0.6 * diffuse);

	float h1 = sampleHeight(texcoord0.st + vec2(0,-SMTEXEL));
	float h2 = sampleHeight(texcoord0.st + vec2(0,SMTEXEL));
	float h3 = sampleHeight(texcoord0.st + vec2(-SMTEXEL,0));
	float h4 = sampleHeight(texcoord0.st + vec2(SMTEXEL,0));

    vec4 contourCol = vec4(1.0,0.5,0.0,1.0);

	col += contourCol * contour(h,h1,h2,h3,h4,500.0) * 0.05;
	col += contourCol * contour(h,h1,h2,h3,h4,50.0) * 0.1;


    out_Colour = vec4(col.rgb,1.0);
  
}
