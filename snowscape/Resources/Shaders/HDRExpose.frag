#version 140
precision highp float;

uniform sampler2D colTex;
uniform sampler2D histogramTex;
uniform float exposure;
uniform float whitelevel;
uniform float blacklevel;

in vec2 texcoord0;
out vec4 out_Colour;

float A = 0.15;
float B = 0.50;
float C = 0.10;
float D = 0.20;
float E = 0.02;
float F = 0.30;
float W = 11.2;

vec3 Uncharted2Tonemap2(vec3 x)
{
   return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
}

vec3 Uncharted2Tonemap(vec3 col)
{
	vec3 colA = col * A;
	return (
				(col * (colA + vec3(C*B)) + vec3(D*E)) / 
				(col * (colA + vec3(B)) + vec3(D*F))
		   ) - vec3(E/F);
}

void main(void)
{
	
	//vec3 col = texture(colTex,texcoord0).rgb;
	vec3 col = textureLod(colTex,texcoord0,0).rgb;

	// chromatic aberration
	//vec2 posFromCentre = texcoord0.xy - vec2(0.5);
	//vec3 col;
	//col.r = textureLod(colTex,texcoord0 + posFromCentre * 0.0,0).r;
	//col.g = textureLod(colTex,texcoord0 + posFromCentre * 0.005,0).g;
	//col.b = textureLod(colTex,texcoord0 + posFromCentre * 0.01,0).b;
//

	// set black level
	col.rgb -= vec3(blacklevel);

	// apply exposure
	//col.rgb = vec3(1.0) - exp(col.rgb * exposure);
	col.rgb *= -exposure;

	// reinhard tone map
	//float whitelevel = 2.0;
	col.rgb = (col.rgb  * (vec3(1.0) + (col.rgb / (whitelevel * whitelevel))  ) ) / (vec3(1.0) + col.rgb);

	// uncharted 2 tonemap - do not do exp and gamma
	//float expBias = exposure;
	//col = Uncharted2Tonemap(col * expBias);
	//vec3 white = 1.0 / Uncharted2Tonemap(vec3(W));
	//col = col*white;
//
	// gamma correct
	//col = sqrt(col.rgb);
	col = pow(col.rgb,vec3(1.0/2.2));

	// TODO: Anti-alias
	/*
	// render histogram
	vec2 p = (texcoord0 - vec2(0.5,0.9)) * vec2(2.2,9.2);
	if (p.x >= 0.0 && p.y >= 0.0 && p.x < 1.0 && p.y < 1.0)
	{
		vec4 h = texture(histogramTex,vec2(p.x,0.0));

		col *= 0.75;
		col.rgb += (vec3(1.0)-step(h.rgb,vec3(p.y))) * 0.2;
		col.rgb += vec3((1.0 - step(h.a,p.y)) * 0.5);
	}*/
	

	// output
	out_Colour = vec4(col,1.0);
}
