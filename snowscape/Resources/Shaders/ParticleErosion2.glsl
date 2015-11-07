//|Common

float heightFromStack(vec4 terrainSample)
{
	//hard + soft + water + dynamic water * wheight
	//return dot(terrainSample, vec4(1.0,1.0,1.0,waterHeightFactor));
	return dot(terrainSample, vec4(1.0,1.0,1.0,0.0));
}

//|ComputeVelocity

#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D particletex;
uniform sampler2D velocitytex;
uniform float texsize;
uniform float carryingCapacityLowpass;
uniform float speedCarryingCoefficient;
uniform float waterHeightFactor;
uniform float fallRand;
uniform float randSeed;

in vec2 texcoord;

out vec4 out_Velocity;

float t = 1.0 / texsize;
float diag = 0.707;

#include "noise.glsl"
#include ".|Common"

float sampleHeight(vec2 pos)
{
	return heightFromStack(texture(terraintex,pos));  
}

void main(void)
{
	vec4 prevvel = texture(velocitytex,texcoord);
	vec4 particle = texture(particletex,texcoord);

	vec3 ofs = vec3(0,-t,t);

	// get our current height
	float h0 = sampleHeight(particle.xy);

	// get neighbouring height differentials - positive means the neighbour is downhill from here
	float hn  =  h0 - sampleHeight(particle.xy + ofs.xy); // x=0, y=-1, z=+1
	float hs  =  h0 - sampleHeight(particle.xy + ofs.xz);
	float hw  =  h0 - sampleHeight(particle.xy + ofs.yx);
	float he  =  h0 - sampleHeight(particle.xy + ofs.zx);
	float hnw = (h0 - sampleHeight(particle.xy + ofs.yy)) * diag; 
	float hne = (h0 - sampleHeight(particle.xy + ofs.zy)) * diag; 
	float hsw = (h0 - sampleHeight(particle.xy + ofs.yz)) * diag; 
	float hse = (h0 - sampleHeight(particle.xy + ofs.zz)) * diag; 

	float maxDownhill = max(max(max(hn,hs),max(hw,he)),max(max(hnw,hne),max(hsw,hse)));

	// detect if we're in a hole (lowest point among neighbours)
	// if we're in a hole, we drop some water in it, either to fill the hole or to deplete a percentage of the particle
	float waterDrop = min(max(0.0,-maxDownhill),particle.a);

	// TODO: if we're moving downhill and there is water here, we should pick some up.

	vec2 fall = vec2(0.0);

	// small amount of random noise in movement
	fall.x = (rand(particle.xy + vec2(randSeed))-0.5) * fallRand;
	fall.y = (rand(particle.xy + vec2(randSeed * 3.19 + 7.36))-0.5) * fallRand;

	fall += vec2(0,-1)  * max(0.0,hn);
	fall += vec2(0,1)   * max(0.0,hs);
	fall += vec2(-1,0)  * max(0.0,hw);
	fall += vec2(1,0)   * max(0.0,he);
	fall += vec2(-1,-1) * max(0.0,hnw);
	fall += vec2(1,-1)  * max(0.0,hne);
	fall += vec2(-1,1)  * max(0.0,hsw);
	fall += vec2(1,1)   * max(0.0,hse);

	fall = normalize(fall);

	// calculate velocity of particle due to slope
	float speed = atan(max(0,maxDownhill));

	// calculate new carrying capacity based on speed and amount of water in particle
	float newCarryingCapacity = speed * speedCarryingCoefficient * particle.a;
	float prevCarryingCapacity = prevvel.b;
		
	// return
	// RG: 2D normalized movement vector 
	// B: carrying capacity
	// A: water drop amount (hole filling)
	out_Velocity = vec4(fall,mix(newCarryingCapacity,prevCarryingCapacity,carryingCapacityLowpass),waterDrop);
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
	texcoord = texture(particletex,vertex.xy).xy;
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

	// return:
	// R: 1.0: particle count
	// G: erosion potential
	// B: deposit amount
	// A: water dropped from particle
	out_Erosion = vec4(1.0, erosionPotential, depositAmount, particle.a);
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
//  Modifies standing water amount
//  Modifies dynamic water depth from particle count (E.r).

float saturationlowpass = 0.9999;
vec3 t = vec3(-1.0/1024.0,0.0,1.0/1024.0);

void main(void)
{
	vec4 terrain = textureLod(terraintex,texcoord,0);
	vec4 erosion = textureLod(erosiontex,texcoord,0);

	//float avgsat = textureLod(terraintex,texcoord + t.xy,0).a;
	//avgsat += textureLod(terraintex,texcoord + t.zy,0).a;
	//avgsat += textureLod(terraintex,texcoord + t.yx,0).a;
	//avgsat += textureLod(terraintex,texcoord + t.yz,0).a;
	//avgsat += terrain.a;
	//avgsat *= 0.19;
	//avgsat += terrain.b * 2.5;

	//TODO: calculate terrain slope, reduce erosion amount on very steep slopes
	//TODO: reduce/eliminate erosion where there is standing water and/or dynamic water

	float hard = terrain.r;
	float soft = terrain.g;
	float water = terrain.b - min(terrain.b,0.0001);   // reduce water due to evaporation TODO: make uniform

	soft += erosion.b;  // add deposit amount to soft, make available for erosion

	// calculate erosion from soft - lesser of potential and amount of soft material
	float softerode = min(soft, erosion.g);
	float harderode = max(erosion.g - softerode,0.0) * hardErosionFactor;
	float waterchange = erosion.a;

	//float saturationrate = 0.1 + log(1.0+terrain.g*0.1) * 0.1;  // saturation rate faster on soft

	out_Terrain = vec4(
		hard - harderode, 
		soft - softerode,
		water + waterchange,

		terrain.a * waterLowpass + erosion.r * waterDepthFactor   // water saturation


		//erosion.a / max(1.0,erosion.r)  //average sediment carried
		//0.0
		//saturationrate * 4.0
		//avgsat
		//mix(terrain.a,avgsat,saturationrate) //* 0.999
		//max((terrain.a * 0.95 + avgsat * 0.05 * 0.25) * saturationlowpass, terrain.a  * 0.9 + min(8.0,erosion.r) * 0.02)
		//mix(min(8.0,erosion.r) * 0.0625 + avgsat*0.25,terrain.a,saturationlowpass)
		//mix(min(4.0,erosion.r),terrain.a,saturationlowpass)  // saturation
		);
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

	// subtract any dropped water
	newParticle.a = max(0.0,newParticle.a - velocity.a); 

    //  move particle - maybe change to intersect with cell boundary
	newParticle.xy = particle.xy + normalize(velocity.xy) * t * deltatime;

	// keep in range
	newParticle.xy = mod(newParticle.xy + vec2(1.0),vec2(1.0));
	//newParticle.a *= velocity.a;

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

	newParticle = particle;
	//newParticle.a = max(0.0,particle.a - particleDeathRate);	

	if (newParticle.a < 0.0001 && newParticle.z < 0.1)
	{
		newParticle.x = rand(particle.xy + vec2(randSeed) + rand(particle.yx * 641.3));
		newParticle.y = rand(particle.yx + vec2(randSeed + 0.073) + rand(particle.xy * 363.3));
		newParticle.z = 0.0;
		newParticle.w = 0.01;  // TODO: make uniform (particle initial water amount)
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


