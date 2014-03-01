#version 140
precision highp float;

uniform sampler2D flowtex;
uniform float texsize;


in vec2 texcoord;

out vec4 out_Velocity;



float t = 1.0 / texsize;


vec4 sampleFlow(vec2 pos,float ofsx, float ofsy)
{
	return texture(flowtex,pos + vec2(ofsx,ofsy));
}

void main(void)
{
	// RGBA = R:top G:right B:bottom A:left

	vec4 f = sampleFlow(texcoord,0.0,0.0);
	vec4 ftop = sampleFlow(texcoord,0.0,-t);
	vec4 fright = sampleFlow(texcoord,t,0.0);
	vec4 fbottom = sampleFlow(texcoord,0.0,t);
	vec4 fleft = sampleFlow(texcoord,-t,0.0);


	out_Velocity = vec4(
	(fleft.g - f.a + f.g - fright.a) * 0.5,
	(ftop.b - f.r + f.b - fbottom.r) * 0.5,
	0.0,0.0);
}
