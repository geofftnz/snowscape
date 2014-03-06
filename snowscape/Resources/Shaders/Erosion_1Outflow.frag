#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D flowtex;
uniform float texsize;
uniform float flowRate;
uniform float flowLowpass;

in vec2 texcoord;

out vec4 out_Flow;



float t = 1.0 / texsize;


float sampleHeight(vec2 pos)
{
	vec4 l = texture(terraintex,pos);
	return l.r + l.g + l.b;
}

float samplePrevCellOutflow(vec2 pos)
{
	vec4 l = texture(flowtex,pos);
	return l.r + l.g + l.b + l.a;
}

void main(void)
{
	// get our current and neighbour heights
	vec4 l = texture(terraintex,texcoord);
	vec4 prevflow = texture(flowtex,texcoord);
	float h = l.r + l.g + l.b;

	float htop = sampleHeight(texcoord + vec2(0,-t));
	float hright = sampleHeight(texcoord + vec2(t,0));
	float hbottom = sampleHeight(texcoord + vec2(0,t));
	float hleft = sampleHeight(texcoord + vec2(-t,0));

	float oftop = samplePrevCellOutflow(texcoord + vec2(0,-t));
	float ofright = samplePrevCellOutflow(texcoord + vec2(t,0));
	float ofbottom = samplePrevCellOutflow(texcoord + vec2(0,t));
	float ofleft = samplePrevCellOutflow(texcoord + vec2(-t,0));

	float ptop = max(0,h - htop);
	float pright = max(0,h - hright);
	float pbottom = max(0,h - hbottom);
	float pleft = max(0,h - hleft);

	float ptotal = ptop + pleft + pright + pbottom;

	// find lowest neighbour and make sure the max water we remove wont drop our current location below the estimated level of our neighbours.
	float minneighbour = min(min(htop-oftop,hbottom-ofbottom),min(hleft-ofleft,hright-ofright));
	float maxdrop = max(0.0,h - minneighbour);

	// l.b has available water - make sure we don't exceed this or the min-neighbour difference calculated above
	float pavailable = min(min(ptotal,l.b),maxdrop);
	float pscale = pavailable * clamp(1.0 / ptotal,0.0,1.0);

	pscale *= flowRate;

	vec4 newoutflow = vec4(ptop, pright, pbottom, pleft) * pscale;

	out_Flow = prevflow * flowLowpass + newoutflow * (1.0 - flowLowpass);
}
