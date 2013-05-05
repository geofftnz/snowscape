#version 140
precision highp float;

uniform sampler2D cloudTexture;
uniform vec3 sunDirection;
uniform vec3 cloudScale;

in vec2 cloudcoord;

out vec4 out_CloudDepth;



void main(void)
{
	float c = texture2D(cloudTexture,cloudcoord).r;	

	out_CloudDepth = vec4(c,cloudcoord.x,cloudcoord.y,1.0);
}
