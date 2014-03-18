#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out vec4 out_Terrain;


void main(void)
{
	vec4 layers = texture(terraintex,texcoord);

	layers.b += 0.0001;

	out_Terrain = layers;
}
