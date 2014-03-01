#version 140
precision highp float;

uniform sampler2D terraintex;
uniform float texsize;
uniform float flowRate;

in vec2 texcoord;

out vec4 out_Flow;



float t = 1.0 / texsize;


float sampleHeight(vec2 pos)
{
	vec4 l = texture(terraintex,pos);
	return l.r + l.g + l.b;
}

void main(void)
{
	// get our current and neighbour heights
	vec4 l = texture(terraintex,texcoord);
	float h = l.r + l.g + l.b;

	float htop = sampleHeight(texcoord + vec2(0,-t));
	float hright = sampleHeight(texcoord + vec2(t,0));
	float hbottom = sampleHeight(texcoord + vec2(0,t));
	float hleft = sampleHeight(texcoord + vec2(-t,0));

	float ptop = max(0,h - htop);
	float pright = max(0,h - hright);
	float pbottom = max(0,h - hbottom);
	float pleft = max(0,h - hleft);

	float ptotal = ptop + pleft + pright + pbottom;

	// l.b has available water - make sure we don't exceed this.
	l.b = 500.0; // temp for debugging
	float pavailable = min(ptotal,l.b);
	float pscale = pavailable / ptotal;

	pscale *= flowRate;

	out_Flow = vec4(ptop, pright, pbottom, pleft) * pscale;
}
