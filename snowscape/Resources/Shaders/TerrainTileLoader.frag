#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out vec4 out_Height;


void main(void)
{
	vec4 t = textureLod(terraintex,texcoord,0);
	//out_Height = vec4(t.r + t.g,0.0,0.0,0.0);
	out_Height = vec4(t.r + t.g + t.b,0.0,0.0,0.0);
}
