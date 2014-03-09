#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out float out_Height;


void main(void)
{
	vec4 h = texture(terraintex,texcoord);
	out_Height = h.r + h.g + h.b;
}
