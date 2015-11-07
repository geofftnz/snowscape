#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out vec4 out_Param;


void main(void)
{
	vec4 t = textureLod(terraintex,texcoord,0);

	vec4 p = vec4(0.0);

	p.r = clamp(t.g * 0.125,0.0,1.0);   // soft material
	p.g = clamp(t.b,0.0,1.0);			// standing water
	//p.g = clamp(t.a,0.0,1.0);			// flowing water
	p.b = clamp(t.a,0.0,1.0);           // other

	out_Param = p;
}
