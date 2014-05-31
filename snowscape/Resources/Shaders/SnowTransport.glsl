/*
	GPU Snow transport model
	(c)2014 Geoff Thornburrow

	Parameters:
	- Wind vector
	- Snowfall rate
	- Temperature (? maybe later for melt/freeze)

	Terrain:
	R: hard
	G: soft
	B: packed snow
	A: powder



	Steps:

	1: Snowfall
	2: Powder slip
	3: Powder compaction

	
*/

//|SnowFall
#version 140
precision highp float;

uniform sampler2D terraintex;
uniform float snowfallrate;
in vec2 texcoord;
out vec4 out_Terrain;


void main(void)
{
	vec4 layers = texture(terraintex,texcoord);

	layers.a += snowfallrate;

	out_Terrain = layers;
}

//|ParticleVelocity
#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D velocitytex;
uniform sampler2D particletex;

uniform float texsize;
uniform vec2 windvelocity;
uniform float lowpass;
uniform float terrainfactor;

in vec2 texcoord;
out vec4 out_Velocity;

float t = 1.0 / texsize;
vec3 tt = vec3(-t,0,t);

float sampleHeight(vec2 pos)
{
	vec4 l = texture(terraintex,pos);
	return l.r + l.g + l.b + l.a;
}

vec3 getNormal(vec2 pos)
{
    float h1 = sampleHeight(pos+tt.yx);
	float h2 = sampleHeight(pos+tt.yz);
    float h3 = sampleHeight(pos+tt.xy);
	float h4 = sampleHeight(pos+tt.zy);
	return normalize(vec3(h3-h4,h1-h2,2.0));
}

void main(void)
{
	vec4 particle = texture(particletex,texcoord);
	vec4 prevvel = texture(velocitytex,texcoord);

	vec3 n = getNormal(particle.xy);

	vec2 newvel = windvelocity + n.xy * terrainfactor;

	out_Velocity = vec4(mix(newvel,prevvel.xy,lowpass),0,0);
}



//|ErosionVertex
#version 140
precision highp float;

uniform sampler2D particletex;

in vec3 vertex;
out vec2 texcoord;
out vec2 particlecoord;

void main(void)
{
	particlecoord = vertex.xy;

	// use vertex as lookup into particle texture to get actual position
	texcoord = textureLod(particletex,vertex,0).xy;
	gl_Position = vec4(texcoord.xy*2.0-1.0,0.0,1.0);
}

//|Erosion
#version 140
precision highp float;

uniform sampler2D particletex;
uniform sampler2D velocitytex;
uniform float deltatime;
uniform float depositRate;
uniform float erosionRate;

in vec2 texcoord;
in vec2 particlecoord;

out vec4 out_Erosion;

void main(void)
{
    //   Calculate particle potential as (new carrying capacity - carrying amount).
    //   Writes R:1 G:potential B:deposit - blend as add
	vec4 particle = textureLod(particletex,particlecoord,0);
	vec4 velocity = textureLod(velocitytex,particlecoord,0);

	//float carryingCapacity = velocity.b;
	//float carrying = particle.b;

	//float erosionPotential = max(carryingCapacity - carrying,0.0) * erosionRate * deltatime;
	//float depositAmount = max(carrying - carryingCapacity,0.0) * depositRate * deltatime;

	out_Erosion = vec4(1.0, 0.0, 0.0, 0.0);
}




//|UpdateParticles
#version 140
precision highp float;

uniform sampler2D particletex;
uniform sampler2D velocitytex;
uniform sampler2D erosiontex;
uniform sampler2D terraintex;

uniform float deltatime;
//uniform float depositRate;
//uniform float erosionRate;
//uniform float hardErosionFactor;
uniform float texsize;

in vec2 texcoord;
out vec4 out_Particle;

float t = 1.0 / texsize;


void main(void)
{
	vec4 particle = textureLod(particletex,texcoord,0);
	vec4 velocity = textureLod(velocitytex,texcoord,0);
	vec2 terraincoord = particle.xy;
	vec4 erosion = textureLod(erosiontex,terraincoord,0);
	vec4 terrain = textureLod(terraintex,terraincoord,0);

	vec4 newParticle = particle;

    //  move particle 
	newParticle.xy = particle.xy + normalize(velocity.xy) * t * deltatime;

	// keep in range
	newParticle.xy = mod(newParticle.xy + vec2(1.0),vec2(1.0));

	out_Particle = newParticle;

}



//|CopyParticles
#version 140
precision highp float;

uniform sampler2D particletex;

in vec2 texcoord;
out vec4 out_Particle;

void main(void)
{
	out_Particle = textureLod(particletex,texcoord,0);
}


//|CopyVelocity
#version 140
precision highp float;

uniform sampler2D velocitytex;

in vec2 texcoord;
out vec4 out_Velocity;

void main(void)
{
	out_Velocity = textureLod(velocitytex,texcoord,0);
}


