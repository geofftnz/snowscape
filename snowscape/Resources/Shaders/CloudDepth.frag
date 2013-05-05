#version 140
precision highp float;

uniform sampler2D cloudTexture;
uniform vec3 sunDirection;
uniform vec3 cloudScale;

in vec2 cloudcoord;

out vec4 out_CloudDepth;



void main(void)
{
	out_CloudDepth = vec4(1.0,cloudcoord.x,cloudcoord.y,1.0);
}
