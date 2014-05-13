//|ComputeVelocity

#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D particletex;
uniform sampler2D velocitytex;
uniform float texsize;
uniform float carryingCapacityLowpass;
uniform float speedCarryingCoefficient;

in vec2 texcoord;

out vec4 out_Velocity;

float t = 1.0 / texsize;

float sampleHeight(vec2 pos)
{
	vec4 l = texture(terraintex,pos);
	return l.r + l.g + l.b;
}

vec3 fallVector(vec2 p)
{
	float h0 = sampleHeight(p);
	float h1 = sampleHeight(p + vec2(0,-t));
	float h2 = sampleHeight(p + vec2(0,t));
	float h3 = sampleHeight(p + vec2(-t,0));
	float h4 = sampleHeight(p + vec2(t,0));

	vec3 f = vec3(0);

	f += vec3(0,-1,h1-h0) * (h0-h1);
	f += vec3(0,1,h2-h0) * (h0-h2);
	f += vec3(-1,0,h3-h0) * (h0-h3);
	f += vec3(1,0,h4-h0) * (h0-h4);

	return normalize(f);
}

void main(void)
{
	vec4 prevvel = texture(velocitytex,texcoord);
	vec4 particle = texture(particletex,texcoord);

	//  Uses slope information from L0 at position P0 to calculate acceleration of particle
	vec3 fall = fallVector(particle.xy);
	vec2 newVelocity = normalize(fall.xy) * step(0.0,-fall.z);

	float speed = max(-fall.z,0.0);
	float newCarryingCapacity = speed * speedCarryingCoefficient * pow(particle.a,0.3);
	float prevCarryingCapacity = prevvel.b;
		
	out_Velocity = vec4(newVelocity.xy,mix(newCarryingCapacity,prevCarryingCapacity,carryingCapacityLowpass),1.0);
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

	float carryingCapacity = velocity.b;
	float carrying = particle.b;

	float erosionPotential = max(carryingCapacity - carrying,0.0) * erosionRate * deltatime;
	float depositAmount = max(carrying - carryingCapacity,0.0) * depositRate * deltatime;

	out_Erosion = vec4(1.0, erosionPotential, depositAmount, 1.0);
}

//|UpdateTerrain
#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D erosiontex;

uniform float hardErosionFactor;
uniform float waterLowpass;
uniform float waterDepthFactor;

in vec2 texcoord;
out vec4 out_Terrain;

//  Adds deposit amount E.b to soft L0.g
//  Subtracts material from soft, then hard.
//  Modifies water depth from particle count (E.r).

void main(void)
{
	vec4 terrain = textureLod(terraintex,texcoord,0);
	vec4 erosion = textureLod(erosiontex,texcoord,0);

	float hard = terrain.r;
	float soft = terrain.g;

	soft += erosion.b;  // add deposit amount to soft, make available for erosion

	// calculate erosion from soft - lesser of potential and amount of soft material
	float softerode = min(soft, erosion.g);
	float harderode = max(erosion.g - softerode,0.0) * hardErosionFactor;

	out_Terrain = vec4(
		hard - harderode, 
		soft - softerode,
		terrain.b * waterLowpass + erosion.r * waterDepthFactor,
		terrain.a);
}

//|UpdateParticles
#version 140
precision highp float;

uniform sampler2D particletex;
uniform sampler2D velocitytex;
uniform sampler2D erosiontex;
uniform sampler2D terraintex;

uniform float deltatime;
uniform float depositRate;
uniform float erosionRate;
uniform float hardErosionFactor;
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

	float carryingCapacity = velocity.b;
	float carrying = particle.b;

    //  Replicate particle potential calc from Step 2 to get deposit/erode potentials.
	float erosionPotential = max(carryingCapacity - carrying,0.0) * erosionRate * deltatime;
	float depositAmount = max(carrying - carryingCapacity,0.0) * depositRate * deltatime;

    //  Subtract deposit amount from carrying amount P0.b, write to P1.b
	newParticle.b = max(carrying - depositAmount,0.0);


    //  Apply same calculation as step 3 to determine how much soft/hard is being eroded from L0(P0.rg).
	float hard = terrain.r;
	float soft = terrain.g;

	soft += erosion.b;  // add deposit amount to soft, make available for erosion

	// calculate erosion from soft - lesser of potential and amount of soft material
	float softerode = min(soft, erosion.g);
	float harderode = max(erosion.g - softerode,0.0) * hardErosionFactor;
	float totaleroded = softerode + harderode;

    //  Add material carried based on particle share of total V0.b / E(P0.rg).g -> P1.b
	float particleerode = totaleroded * (erosionPotential / erosion.g);
	newParticle.b += particleerode;


    //  move particle
	newParticle.xy = particle.xy + normalize(velocity.xy) * t * deltatime;
	newParticle.a *= velocity.a;

	out_Particle = newParticle;

}

//|CopyParticles
#version 140
precision highp float;

uniform sampler2D particletex;

uniform float particleDeathRate;
uniform float randSeed;

in vec2 texcoord;
out vec4 out_Particle;

#include "noise.glsl"

void main(void)
{
	vec4 particle = textureLod(particletex,texcoord,0);
	vec4 newParticle;

	newParticle.xyz = particle.xyz;
	newParticle.a = max(0.0,particle.a - particleDeathRate);	

	if (newParticle.a < 0.001 && newParticle.z < 0.00001)
	{
		newParticle.x = rand(particle.xy + vec2(randSeed));
		newParticle.y = rand(particle.yx + vec2(randSeed + 0.073));
		newParticle.z = 0.0;
		newParticle.w = 1.0;
	}

	out_Particle = newParticle;
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


