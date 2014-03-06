#version 140
precision highp float;

uniform sampler2D terraintex;

uniform float texsize;
uniform float maxdiff;
uniform float sliprate;

in vec2 texcoord;

out vec4 out_Slip;


float t = 1.0 / texsize;


float sampleHeight(vec2 pos)
{
	vec4 l = texture(terraintex,pos);
	return l.r + l.g;
}

void main(void)
{
	// get our current and neighbour heights
	vec4 l = texture(terraintex,texcoord);
	float h = l.r + l.g - maxdiff;

	float htop = sampleHeight(texcoord + vec2(0,-t));
	float hright = sampleHeight(texcoord + vec2(t,0));
	float hbottom = sampleHeight(texcoord + vec2(0,t));
	float hleft = sampleHeight(texcoord + vec2(-t,0));

	float ptop = max(0,h - htop);
	float pright = max(0,h - hright);
	float pbottom = max(0,h - hbottom);
	float pleft = max(0,h - hleft);

	float ptotal = ptop + pleft + pright + pbottom;

	// find lowest neighbour and make sure the max water we remove wont drop our current location below the estimated level of our neighbours.
	//float minneighbour = min(min(htop-oftop,hbottom-ofbottom),min(hleft-ofleft,hright-ofright));
	//float maxdrop = max(0.0,h - minneighbour);

	// l.g has available loose material - make sure we don't exceed this or the min-neighbour difference calculated above
	float pavailable = min(ptotal,l.g); //min(min(ptotal,l.b),maxdrop);
	float pscale = pavailable * clamp(1.0 / ptotal,0.0,1.0);

	pscale *= sliprate;

	out_Slip = vec4(ptop, pright, pbottom, pleft) * pscale;
}
