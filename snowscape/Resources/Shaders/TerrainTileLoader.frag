#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out vec4 out_Height;

uniform float waterHeightScale;

void main(void)
{
	vec4 t = textureLod(terraintex,texcoord,0);
	//out_Height = vec4(t.r + t.g,0.0,0.0,0.0);
	out_Height = vec4(t.r + t.g + t.b * waterHeightScale,0.0,0.0,0.0);
}
