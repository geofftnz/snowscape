#version 140
precision highp float;

uniform sampler2D flowtex;
uniform sampler2D flowdtex;
uniform sampler2D velocitytex;
uniform float texsize;
uniform float vdecay;
uniform float vadd;

in vec2 texcoord;

out vec4 out_Velocity;



float t = 1.0 / texsize;
float diag = 0.707;


vec4 sampleFlow(vec2 pos,float ofsx, float ofsy)
{
	return texture(flowtex,pos + vec2(ofsx,ofsy));
}

vec4 sampleFlowd(vec2 pos,float ofsx, float ofsy)
{
	return texture(flowdtex,pos + vec2(ofsx,ofsy));
}

void main(void)
{
	// RGBA = R:top G:right B:bottom A:left
	// RGBA = R:topright G:bottomright B:bottomleft A:topleft

	vec4 f = sampleFlow(texcoord,0.0,0.0);
	vec4 fd = sampleFlowd(texcoord,0.0,0.0);
	vec4 prevvel = texture(velocitytex,texcoord);
	
	float infromtop = sampleFlow(texcoord,0.0,-t).b;
	float infromright = sampleFlow(texcoord,t,0.0).a;
	float infrombottom = sampleFlow(texcoord,0.0,t).r;
	float infromleft = sampleFlow(texcoord,-t,0.0).g;
	float infromtopright = sampleFlow(texcoord,t,-t).b;
	float infrombottomright = sampleFlow(texcoord,t,t).a;
	float infrombottomleft = sampleFlow(texcoord,-t,t).r;
	float infromtopleft = sampleFlow(texcoord,-t,-t).g;

	vec2 v = vec2(0.0);

	// left/right
	v += vec2(1.0,0.0) * (infromleft + f.g - f.a - infromright) * 0.5;

	// up/down
	v += vec2(0.0,1.0) * (infromtop + f.b - f.r - infrombottom) * 0.5;

	// topleft/bottomright
	v += vec2(1.0,1.0) * (infromtopleft + fd.g - fd.a - infrombottomright) * diag * 0.5;

	// topright/bottomleft
	v += vec2(-1.0,1.0) * (infromtopright + fd.b - fd.r - infrombottomleft) * diag * 0.5;

	out_Velocity = prevvel * vdecay + vec4(v,0.0,0.0) * vadd;
}
