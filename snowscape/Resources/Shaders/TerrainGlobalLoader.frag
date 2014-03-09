#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out vec4 out_Height;


void main(void)
{
	vec4 h = textureLod(terraintex,texcoord,0);
	out_Height = vec4(h.r + h.g + h.b,0.0,0.0,0.0);
}
