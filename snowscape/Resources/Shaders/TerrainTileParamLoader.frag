#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out vec4 out_Param;


void main(void)
{
	vec4 t = textureLod(terraintex,texcoord,0);

	vec4 p = vec4(0.0);

	p.r = clamp(t.g / 64.0,0.0,1.0);
	p.g = clamp(t.b,0.0,1.0);
	p.b = clamp(t.a * 32.0,0.0,1.0);

	out_Param = p;
}
