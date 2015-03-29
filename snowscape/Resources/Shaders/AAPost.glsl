//|vs
#version 140

in vec3 vertex;
noperspective out vec2 tex0;

void main() {

	gl_Position = vec4(vertex.xy * 2.0 - 1.0,0.0,1.0);
	tex0 = vertex.xy;
}

//|fs
#version 140
precision highp float;

uniform sampler2D inputTex;
uniform sampler2D lastFrameTex;
uniform float frameBlend;

noperspective in vec2 tex0;

out vec4 out_Col;

const vec3 luminance = vec3(0.2126,0.7152,0.0722);


void main(void)
{
	float fblend = frameBlend;
	vec3 c = textureLod(inputTex,tex0,0).rgb;
	vec4 lf = textureLod(lastFrameTex,tex0,0);

	//float clum = dot(c,luminance);

	//if (abs(clum - lf.w) > 0.8) fblend = 1.0;
	//c.g = 1.0;
	//lf.r = 1.0;
	vec3 outc = mix(lf.rgb,c.rgb,fblend);

	out_Col = vec4(outc, dot(outc, luminance));
	//out_Col = (lf - c) * 10.0 + 0.5 ;
}



//|vsout
#version 140

in vec3 vertex;
noperspective out vec2 tex0;

void main() {

	gl_Position = vec4(vertex.xy * 2.0 - 1.0,0.0,1.0);
	tex0 = vertex.xy;
}


//|fsout
#version 140
precision highp float;

uniform sampler2D inputTex;
noperspective in vec2 tex0;

out vec4 out_Col;


void main(void)
{
	vec4 c = textureLod(inputTex,tex0,0);

	//c.rb = tex0;
	out_Col = c;
}

