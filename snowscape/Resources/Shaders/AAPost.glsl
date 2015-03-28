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


void main(void)
{
	vec4 c = textureLod(inputTex,tex0,0);
	vec4 lf = textureLod(lastFrameTex,tex0,0);

	out_Col = c;
}




