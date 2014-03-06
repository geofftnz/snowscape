#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out vec4 out_Terrain;


void main(void)
{
	out_Terrain = texture(terraintex,texcoord);
}
