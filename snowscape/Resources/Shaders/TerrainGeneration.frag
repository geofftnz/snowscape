#version 140
precision highp float;

uniform sampler2D heightTex;
uniform sampler2D shadeTex;
uniform vec2 resolution;
uniform float tex0_scale;

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

/*
float contour(float h0, float h1,float h2, float h3, float h4, float contourscale)
{
	float b0 = floor(h0*contourscale);
	float b1 = floor(h1*contourscale);
	float b2 = floor(h2*contourscale);
	float b3 = floor(h3*contourscale);
	float b4 = floor(h4*contourscale);

	return (b0>b1 || b0>b2 || b0>b3 || b0>b4) ? 1.0:0.0;
}*/

float cf(vec2 p,float scale)
{
	float h = sampleHeight(p);

	return h-floor((h+0.5)*scale)/scale;

	//return mod(*scale,1.0);
}

vec2 grad(vec2 p, float scale, float sampleOfs)
{
	vec2 h = vec2(sampleOfs,0.0);
	return vec2(cf(p + h.xy,scale) - cf(p - h.xy,scale),cf(p + h.yx,scale) - cf(p - h.yx,scale)) / (2.0*h.x);
}

float contour(vec2 p, float scale, float sampleOfs)
{
	float h = cf(p,scale);
	vec2 g = grad(p,scale,sampleOfs);
	//float de = abs(h) / length(g);
	float de = abs(h) / sqrt(1.0 + dot(g,g));

	return 1.0-smoothstep(0.05,0.15,de*4);
}


vec4 filterShade(vec2 p, float sampleOfs)
{
	vec2 h = vec2(sampleOfs,0.0);
	vec4 s = texture2D(shadeTex,p+h.xy);
	s += texture2D(shadeTex,p-h.xy);
	s += texture2D(shadeTex,p+h.yx);
	s += texture2D(shadeTex,p-h.yx);
	s += texture2D(shadeTex,p)*2.0;
	return s / 6.0;
}


float cfw(vec2 p)
{
	float h = filterShade(p,1.0/1024.0).g; //texture2D(shadeTex,p).g;

	return h;

	//return mod(*scale,1.0);
}

vec2 gradw(vec2 p, float sampleOfs)
{
	vec2 h = vec2(sampleOfs,0.0);
	return normalize(vec2(cfw(p + h.xy) - cfw(p - h.xy),cfw(p + h.yx) - cfw(p - h.yx)) / (2.0*h.x));
}

float contourw(vec2 p,  float sampleOfs)
{
	float h = cfw(p);
	vec2 g = gradw(p,sampleOfs);
	//float de = abs(h) / length(g);
	float de = abs(h) / sqrt(1.0 + dot(g,g));

	return smoothstep(0.05,0.15,de);
	//return h;
}





void main(void)
{
	vec2 p = texcoord0.xy;
	float sampleOfs = (tex0_scale / resolution.x);

    vec4 contourCol = vec4(0.898,0.549,0.098,1.0);
	vec4 riverCol = vec4(0.0,0.517,0.737,1.0);
	vec4 waterCol = vec4(0.819,0.905,0.960,1.0);

	//vec4 s = filterShade(texcoord0.st,sampleOfs); //  texture2D(shadeTex,texcoord0.st);
	float h = sampleHeight(p);
    vec3 n = getNormal(p);
    vec3 l = normalize(vec3(0.5,0.5,0.3));

    float diffuse = 0.8 + 0.2 * clamp(dot(n,l) * 0.5 + 0.7,0,1);

    vec4 col = vec4(0.98,0.98,0.98,1.0);

	//vec2 gr = gradw(p,sampleOfs);
	//col.rg = (gr * 0.5) + 0.5;


	//col = mix(col,vec4(0.0,1.0,0.0,1.0),s.g);

	float water = contourw(p,sampleOfs);
	col = mix(col,waterCol,water);

	//float river = contourw(p,sampleOfs);
	//col = mix(col,riverCol,river*0.6);


	col *= diffuse;

    float contourAmount = 0.0;
	contourAmount += contour(p,100.0,sampleOfs);	
	contourAmount += contour(p,20.0,sampleOfs);
    contourAmount = clamp(contourAmount,0.0,1.0);

    col = mix(col,contourCol,contourAmount*0.6);

    out_Colour = vec4(col.rgb,1.0);
  
}