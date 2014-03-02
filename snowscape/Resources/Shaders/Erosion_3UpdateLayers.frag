#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D flowtex;
uniform sampler2D velocitytex;

uniform float texsize;
uniform float capacitybias;
uniform float capacityscale;
uniform float rockerodability;
uniform float erosionfactor;
uniform float depositfactor;
uniform float evaporationfactor;

in vec2 texcoord;

out vec4 out_Terrain;



float t = 1.0 / texsize;

float sampleHeight(vec2 pos)
{
	vec4 l = texture(terraintex,pos);
	return l.r + l.g + l.b;
}

vec4 sampleFlow(vec2 pos)
{
	return texture(flowtex,pos);
}

vec3 terrainGradient(vec2 p)
{
	// up/down
	float h1 = sampleHeight(p + vec2(0,-t));
	float h2 = sampleHeight(p + vec2(0,t));

	// left/right
	float h3 = sampleHeight(p + vec2(-t,0));
	float h4 = sampleHeight(p + vec2(t,0));

	//return vec2(h4-h3,h2-h1) * 0.5;
	return normalize(vec3(h3-h4,2.0,h1-h2));
}

void main(void)
{
	// flow RGBA = R:top G:right B:bottom A:left

	// R:hard G:soft B:water A:suspended
	vec4 layers = texture(terraintex,texcoord);
	vec3 grad = terrainGradient(texcoord);
	vec2 velocity = texture(velocitytex,texcoord).rg;

	float capacity = capacityscale * layers.b * max(capacitybias,dot(grad, vec3(0,-1,0))) * length(velocity);

	float erosionamount = erosionfactor * layers.b * max(0, capacity - layers.a);
	float depositamount = depositfactor * layers.b * max(0, layers.a - capacity);

	// erode
	// erode from soft material first - take the lesser of erosionamount and soft material
	float softerode = min(layers.g, erosionamount);
	layers.g -= softerode;
	erosionamount -= softerode;
	layers.a += softerode;

	// erode from hard material
	float harderode = erosionamount * rockerodability;
	layers.r -= harderode;
	layers.a += harderode;

	// deposit
	layers.g += depositamount;
	layers.a -= depositamount;

	// evaporation
	//float evaporationamount = min(layers.b, 

	// move water according to flows

	//vec4 outflow = sampleFlow(texcoord);
//
	//// add outflows from surrounding blocks.
	//// left cell, take right outflow
	//layers.b += sampleFlow(texcoord + vec2(-t,0)).g;
//
	//// right cell, take left outflow
	//layers.b += sampleFlow(texcoord + vec2(t,0)).a;
//
	//// top cell, take bottom outflow
	//layers.b += sampleFlow(texcoord + vec2(0,-t)).b;
//
	//// bottom cell, take top outflow
	//layers.b += sampleFlow(texcoord + vec2(0,t)).r;
//
	//// subtract outflows
	//layers.b -= min(layers.b,outflow.r + outflow.g + outflow.b + outflow.a);
//
	out_Terrain = layers;
	

	/*
	vec4 f = sampleFlow(texcoord,0.0,0.0);
	vec4 ftop = sampleFlow(texcoord,0.0,-t);
	vec4 fright = sampleFlow(texcoord,t,0.0);
	vec4 fbottom = sampleFlow(texcoord,0.0,t);
	vec4 fleft = sampleFlow(texcoord,-t,0.0);


	out_Velocity = vec4(
	(fleft.g - f.a + f.g - fright.a) * 0.5,
	(ftop.b - f.r + f.b - fbottom.r) * 0.5,
	0.0,0.0); */
}
