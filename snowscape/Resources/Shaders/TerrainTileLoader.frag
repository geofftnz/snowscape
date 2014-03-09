#version 140
precision highp float;

uniform sampler2D terraintex;
//uniform sampler2D paramtex;
in vec2 texcoord;
out float out_Height;
out float out_Param;


void main(void)
{
	vec4 t = texture(terraintex,texcoord);
	out_Height = t.r + t.g + t.b;

	vec4 p = vec4(0.0);

	p.r = clamp(t.g / 32.0,0.0,1.0);
	p.g = clamp(t.b / 4.0,0.0,1.0);
	p.b = clamp(t.a * 32.0,0.0,1.0);

	out_Param = p;
}
