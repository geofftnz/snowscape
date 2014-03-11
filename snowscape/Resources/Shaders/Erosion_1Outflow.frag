#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D flowtex;
uniform sampler2D flowdtex;
uniform float texsize;
uniform float flowRate;
uniform float flowLowpass;
uniform float dt;

in vec2 texcoord;

out vec4 out_Flow;
out vec4 out_FlowD;



float t = 1.0 / texsize;
float diagfactor = 0.707;

float sampleHeight(vec2 pos)
{
	vec4 l = texture(terraintex,pos);
	return l.r + l.g + l.b;
}

float samplePrevCellOutflow(vec2 pos)
{
	vec4 l = texture(flowtex,pos);
	vec4 ld = texture(flowdtex,pos);
	return l.r + l.g + l.b + l.a + 
			(ld.r + ld.g + ld.b + ld.a) * diagfactor;
}

void main(void)
{
	// get our current and neighbour heights
	vec4 l = texture(terraintex,texcoord);
	vec4 prevflow = texture(flowtex,texcoord);
	vec4 prevflowd = texture(flowdtex,texcoord);
	float h = l.r + l.g + l.b;

	float htop = sampleHeight(texcoord + vec2(0,-t));
	float hright = sampleHeight(texcoord + vec2(t,0));
	float hbottom = sampleHeight(texcoord + vec2(0,t));
	float hleft = sampleHeight(texcoord + vec2(-t,0));

	float htopright = sampleHeight(texcoord + vec2(t,-t));
	float hbottomright = sampleHeight(texcoord + vec2(t,t));
	float hbottomleft = sampleHeight(texcoord + vec2(-t,t));
	float htopleft = sampleHeight(texcoord + vec2(-t,-t));

	/*
	float oftop = samplePrevCellOutflow(texcoord + vec2(0,-t));
	float ofright = samplePrevCellOutflow(texcoord + vec2(t,0));
	float ofbottom = samplePrevCellOutflow(texcoord + vec2(0,t));
	float ofleft = samplePrevCellOutflow(texcoord + vec2(-t,0));

	float oftopright = samplePrevCellOutflow(texcoord + vec2(t,-t));
	float ofbottomright = samplePrevCellOutflow(texcoord + vec2(t,t));
	float ofbottomleft = samplePrevCellOutflow(texcoord + vec2(-t,t));
	float oftopleft = samplePrevCellOutflow(texcoord + vec2(-t,-t));
	*/

	float ptop = max(0,h - htop);
	float pright = max(0,h - hright);
	float pbottom = max(0,h - hbottom);
	float pleft = max(0,h - hleft);
	float ptopright = max(0,h - htopright);
	float pbottomright = max(0,h - hbottomright);
	float pbottomleft = max(0,h - hbottomleft);
	float ptopleft = max(0,h - htopleft);

	float ptotal = ptop + pleft + pright + pbottom + ptopright + pbottomright + pbottomleft + ptopleft;

	// find lowest neighbour and make sure the max water we remove wont drop our current location below the estimated level of our neighbours.
	// uses previous flow rate to estimate neighbour height.

	float minneighbour = min(
							min(
								min(htop,hbottom),
								min(hleft,hright)
							),
							min(
								min(htopright,hbottomright),
								min(hbottomleft,htopleft)
							)
						);

	float maxdrop = max(0.0,h - minneighbour);

	// l.b has available water - make sure we don't exceed this or the min-neighbour difference calculated above
	float pavailable = min(min(ptotal,l.b),maxdrop);
	//float pavailable = min(ptotal,l.b);
	float pscale = pavailable * clamp(1.0 / ptotal,0.0,1.0);

	pscale *= flowRate;

	vec4 newoutflow = vec4(ptop, pright, pbottom, pleft);
	vec4 newoutflowd = vec4(ptopright, pbottomright, pbottomleft, ptopleft);

	out_Flow = max(vec4(0.0),prevflow + newoutflow * dt) * pscale;
	out_FlowD = max(vec4(0.0), prevflowd + newoutflowd * dt) * pscale;
}
