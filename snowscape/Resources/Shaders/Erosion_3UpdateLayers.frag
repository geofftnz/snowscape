#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D flowtex;
uniform sampler2D flowdtex;
uniform sampler2D velocitytex;

uniform float texsize;
uniform float capacitybias;
uniform float capacityscale;
uniform float rockerodability;
uniform float erosionfactor;
uniform float depositfactor;

in vec2 texcoord;

out vec4 out_Terrain;
out vec4 out_Vis;



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
	vec4 vis = vec4(0.0);

	// R:hard G:soft B:water A:suspended
	vec4 layers = texture(terraintex,texcoord);
	vec3 grad = terrainGradient(texcoord);
	vec2 velocity = texture(velocitytex,texcoord).xy;

	float capacity = capacityscale * min(1.0,0.001+max(0.0,layers.b * 2.0)) * ( capacitybias + dot(grad, vec3(0,1,0))) * dot(velocity,velocity);
	//float capacity = capacityscale * max(capacitybias,dot(grad, vec3(0.0,1.0,0.0))) * length(velocity);
	//float capacity = capacityscale * layers.b * length(velocity);
	//float capacity = capacitybias + capacityscale * length(velocity);

	layers.a = max(0,layers.a); // TODO: why is this negative infinity?

	float erosionamount = erosionfactor * max(0.0, capacity - layers.a);
	float depositamount = depositfactor * min(layers.a,depositfactor * max(0.0, layers.a - capacity));


	vis.r = erosionamount;
	vis.g = depositamount;
	vis.b = capacity;

	// erode
	// erode from soft material first - take the lesser of erosionamount and soft material
	float softerode = min(layers.g, erosionamount);
	layers.g -= softerode;
	erosionamount -= softerode;
	layers.a += softerode;

	// erode from hard material
	float harderode = max(0,erosionamount) * rockerodability;
	layers.r -= harderode;
	layers.a += harderode;

	// deposit
	depositamount = max(0.0,min(layers.a, depositamount));
	layers.g += depositamount;
	layers.a -= depositamount;

	//vis.g = layers.a * 64.0;

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
	out_Vis = vis;
	
}
