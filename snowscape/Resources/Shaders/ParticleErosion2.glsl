//|Common

float heightFromStack(vec4 terrainSample)
{
	//hard + soft + water + dynamic water * wheight
	//return dot(terrainSample, vec4(1.0,1.0,1.0,waterHeightFactor));
	return dot(terrainSample, vec4(1.0,1.0,1.0,0.0));
}

//|AnalyseTerrain
#version 140
precision highp float;
uniform sampler2D terraintex;
uniform float texsize;
uniform float fallRand;
uniform float randSeed;
in vec2 texcoord;
out vec4 out_Limits;
float t = 1.0 / texsize;
float diag = 0.707;

#include "noise.glsl"
#include ".|Common"

float sampleGroundHeight(vec2 pos)
{
	return dot(texture(terraintex,pos), vec4(1.0,1.0,1.0,0.0));  // hard + soft only
}
vec2 sampleGroundWaterHeight(vec2 pos)
{
	vec4 h = texture(terraintex,pos);
	return vec2(
		dot(h, vec4(1.0,1.0,1.0,0.0)),   // x: hard + soft
		dot(h, vec4(1.0,1.0,1.0,0.0))
		);  // y: hard + soft + water
}

void main(void)
{
	vec3 ofs = vec3(0,-t,t);

	// get our current height
	// x=0, y=-1, z=+1
	vec2 ter_0  = sampleGroundWaterHeight(texcoord);
	vec2 ter_n  = sampleGroundWaterHeight(texcoord + ofs.xy); 
	vec2 ter_s  = sampleGroundWaterHeight(texcoord + ofs.xz);
	vec2 ter_w  = sampleGroundWaterHeight(texcoord + ofs.yx);
	vec2 ter_e  = sampleGroundWaterHeight(texcoord + ofs.zx);
	vec2 ter_nw = sampleGroundWaterHeight(texcoord + ofs.yy); 
	vec2 ter_ne = sampleGroundWaterHeight(texcoord + ofs.zy); 
	vec2 ter_sw = sampleGroundWaterHeight(texcoord + ofs.yz); 
	vec2 ter_se = sampleGroundWaterHeight(texcoord + ofs.zz); 


	// get neighbouring height differentials - positive is downhill
	// we ignore water when calculating movement direction.
	float hn  =  ter_0.x - ter_n.x ; 
	float hs  =  ter_0.x - ter_s.x ;
	float hw  =  ter_0.x - ter_w.x ;
	float he  =  ter_0.x - ter_e.x ;
	float hnw = (ter_0.x - ter_nw.x) * diag; 
	float hne = (ter_0.x - ter_ne.x) * diag; 
	float hsw = (ter_0.x - ter_sw.x) * diag; 
	float hse = (ter_0.x - ter_se.x) * diag; 

	vec2 fall = vec2(0.0);

	// small amount of random noise in movement
	fall.x = (rand(texcoord + vec2(randSeed))-0.5) * fallRand;
	fall.y = (rand(texcoord + vec2(randSeed * 3.19 + 7.36))-0.5) * fallRand;

	fall += vec2(0,-1)  * max(0.0,hn);
	fall += vec2(0,1)   * max(0.0,hs);
	fall += vec2(-1,0)  * max(0.0,hw);
	fall += vec2(1,0)   * max(0.0,he);
	fall += vec2(-1,-1) * max(0.0,hnw);
	fall += vec2(1,-1)  * max(0.0,hne);
	fall += vec2(-1,1)  * max(0.0,hsw);
	fall += vec2(1,1)   * max(0.0,hse);

	fall = normalize(fall);

	// recalculate differences with water included
	hn  =  ter_0.y -  ter_n.y ; 
	hs  =  ter_0.y -  ter_s.y ;
	hw  =  ter_0.y -  ter_w.y ;
	he  =  ter_0.y -  ter_e.y ;
	hnw = (ter_0.y - ter_nw.y) * diag;
	hne = (ter_0.y - ter_ne.y) * diag;
	hsw = (ter_0.y - ter_sw.y) * diag;
	hse = (ter_0.y - ter_se.y) * diag;

	float lowestNeighbourDiff = max(max(max(hn,hs),max(hw,he)),max(max(hnw,hne),max(hsw,hse)));
	float highestNeighbourDiff = min(min(min(hn,hs),min(hw,he)),min(min(hnw,hne),min(hsw,hse)));


	// R: max erosion
	// G: max deposit
	// B: fall direction
	// A: hole depth
	out_Limits = vec4(
		lowestNeighbourDiff,
		-highestNeighbourDiff,
		atan(fall.y,fall.x),
		max(0.0,-lowestNeighbourDiff)
	);
}


//|ComputeVelocity

#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D particletex;
uniform sampler2D velocitytex;
uniform sampler2D limittex;
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

	// get our current terrain and analysis from previous step
	vec4 terrain = texture(terraintex,particle.xy);
	vec4 limit = texture(limittex,particle.xy);

	float maxDownhill = limit.r;

	// detect if we're in a hole (lowest point among neighbours)
	// if we're in a hole, we drop some water in it, either to fill the hole or to deplete a percentage of the particle
	float holefill = limit.a; 

	// drop water up to fill the hole, up to 50% of the amount that the particle is carrying
	float waterdrop = min(holefill,particle.a * 0.5);
	particle.a = max(0.0,particle.a - waterdrop);

	// drop 10% of particle water if we're in water
	float waterdrop2 = smoothstep(0.0,0.2,terrain.b) * particle.a * 0.1;
	particle.a = max(0.0,particle.a - waterdrop2);

	// drop some water if we're on flat or near-flat terrain.
	//float waterdrop3 = (1.0 - smoothstep(0.0,0.001,max(0.0,maxDownhill))) * particle.a * 0.01;
	//particle.a = max(0.0,particle.a - waterdrop3);

	waterdrop += waterdrop2;
	//waterdrop += waterdrop3;

	vec2 fall = vec2(cos(limit.b),sin(limit.b));


	// calculate velocity of particle due to slope
	float maxfall = max(0,maxDownhill);
	float speed = atan(maxfall) / 1.570796326794;

	// reduce speed when we hit standing water
	//speed /= (1.0 + terrain.b * 10.0);

	float potential = speed / (0.5 + maxfall*maxfall);

	// reduce erosion potential if there is water in our cell
	// TODO: add factor here, make uniform
	//potential = potential / (1.0 + terrain.b*10.0);

	// reduce erosion potential where there is flowing water
	// TODO: add factor here, make uniform
	//potential = potential / (1.0 + terrain.a*2.0);


	// if we're filling a hole, we cannot erode
	//if (holefill > 0.0)
	//{
	//	potential = 0.0;
	//	speed = 0.0;
	//}

	// calculate new carrying capacity based on speed and amount of water in particle
	float newCarryingCapacity = speed * speedCarryingCoefficient * particle.a;
	float prevCarryingCapacity = prevvel.b;
		
	// return
	// R: Angle
	// G: Erosion potential
	// B: carrying capacity
	// A: amount to fill hole (and die)
	out_Velocity = vec4(limit.b,potential,mix(newCarryingCapacity,prevCarryingCapacity,carryingCapacityLowpass),waterdrop);
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

	float erosionPotentialModifier = velocity.g;
	float carryingCapacity = velocity.b;
	float carrying = particle.b;

	float erosionPotential = max(carryingCapacity - carrying,0.0) * erosionPotentialModifier * erosionRate * deltatime;
	float depositAmount = max(carrying - carryingCapacity,0.0);// * depositRate * deltatime;

	// return:
	// R: 1.0: particle count
	// G: erosion potential
	// B: deposit amount
	// A: water drop amount
	out_Erosion = vec4(1.0, erosionPotential, depositAmount, velocity.a);
}

//|UpdateTerrain
#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D erosiontex;
uniform sampler2D limittex;

uniform float hardErosionFactor;
uniform float waterLowpass;
uniform float waterDepthFactor;

in vec2 texcoord;
out vec4 out_Terrain;

#include ".|Common"

//  Adds deposit amount E.b to soft L0.g
//  Subtracts material from soft, then hard.
//  Modifies standing water amount
//  Modifies dynamic water depth from particle count (E.r).

//TODO: calculate a max-holefill map that contains the amount of material required to bring a hole up to 
//      the level of its nearest neighbour - this will then be used to clamp hole filling algorithm.

float saturationlowpass = 0.9999;
vec3 t = vec3(-1.0/1024.0,0.0,1.0/1024.0);

void main(void)
{
	vec4 terrain = textureLod(terraintex,texcoord,0);
	vec4 erosion = textureLod(erosiontex,texcoord,0);
	vec4 limit = textureLod(limittex,texcoord,0);

	//float avgsat = textureLod(terraintex,texcoord + t.xy,0).a;
	//avgsat += textureLod(terraintex,texcoord + t.zy,0).a;
	//avgsat += textureLod(terraintex,texcoord + t.yx,0).a;
	//avgsat += textureLod(terraintex,texcoord + t.yz,0).a;
	//avgsat += terrain.a;
	//avgsat *= 0.19;
	//avgsat += terrain.b * 2.5;

	float hard = terrain.r;
	float soft = terrain.g;
	float water = terrain.b;// + erosion.a;
	
	//water -= min(water,0.0001);   // reduce water due to evaporation TODO: make uniform

	float maxerosion = limit.r;
	float maxdeposit = limit.g;// + 0.01 + terrain.b*0.5;
	//float holedepth = limit.a;

	soft += erosion.b;  // add deposit amount to soft, make available for erosion

	// calculate erosion from soft - lesser of potential and amount of soft material
	// clamp amount at erosion limit
	float softerode = min(maxerosion, min(soft, erosion.g));
	
	// erode any remaining potential from hard, up to limit less softerode
	float harderode = min(maxerosion - softerode, max(erosion.g - softerode,0.0) * hardErosionFactor);

	// calculate how much water we're giving up to particles
	// get our old and new heights
	float oldHeight = heightFromStack(terrain);
	float newHeight = heightFromStack(vec4(hard - harderode,soft - softerode,water,0.0));
	float overhang = max(0.0,max(0.0,limit.r) + (newHeight - oldHeight));
	float wateroverhang = min(overhang, water);
	water -= wateroverhang * step(0.5,erosion.r); // only move water if there are particles present
	water += erosion.a; // add water inflowing to this location
	

	out_Terrain = vec4(
		hard - harderode, 
		soft - softerode,
		water,

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
uniform sampler2D limittex;

uniform float deltatime;
uniform float depositRate;
uniform float erosionRate;
uniform float hardErosionFactor;
uniform float texsize;

in vec2 texcoord;
out vec4 out_Particle;

float t = 1.0 / texsize;

#include ".|Common"

void main(void)
{
	vec4 particle = textureLod(particletex,texcoord,0);
	vec4 velocity = textureLod(velocitytex,texcoord,0);
	vec2 terraincoord = particle.xy;
	vec4 erosion = textureLod(erosiontex,terraincoord,0);
	vec4 terrain = textureLod(terraintex,terraincoord,0);
	vec4 limit = textureLod(limittex,terraincoord,0);

	vec4 newParticle = particle;

	float erosionPotentialModifier = velocity.g;
	float carryingCapacity = velocity.b;
	float carrying = particle.b;

    //  Replicate particle potential calc from Step 2 to get deposit/erode potentials.
	//float erosionPotential = max(carryingCapacity - carrying,0.0) * erosionRate * deltatime;
	//float depositAmount = max(carrying - carryingCapacity,0.0) * depositRate * deltatime;
	float erosionPotential = max(carryingCapacity - carrying,0.0) * erosionPotentialModifier * erosionRate * deltatime;
	float depositAmount = max(carrying - carryingCapacity,0.0) * depositRate * deltatime;

    //  Subtract deposit amount from carrying amount P0.b, write to P1.b
	newParticle.b = max(carrying - depositAmount,0.0);


    //  Apply same calculation as step 3 to determine how much soft/hard is being eroded from L0(P0.rg).
	float hard = terrain.r;
	float soft = terrain.g;
	float water = terrain.b;// + erosion.a;

	soft += erosion.b;  // add deposit amount to soft, make available for erosion

	float maxerosion = limit.r;
	float maxdeposit = limit.g + 0.01 + terrain.b*0.5;

	// calculate erosion from soft - lesser of potential and amount of soft material
	// clamp amount at erosion limit
	float softerode = min(maxerosion, min(soft, erosion.g));
	// erode any remaining potential from hard, up to limit less softerode
	float harderode = min(maxerosion - softerode, max(erosion.g - softerode,0.0) * hardErosionFactor);
	float totaleroded = softerode + harderode;

    //  Add material carried based on particle share of total V0.b / E(P0.rg).g -> P1.b
	float particleerode = totaleroded * (erosionPotential / erosion.g);
	newParticle.b += particleerode;

	// calculate amount of water picked up from overhanging water
	float oldHeight = heightFromStack(terrain);
	float newHeight = heightFromStack(vec4(hard - harderode,soft - softerode,water,0.0));
	float overhang = max(0.0,max(0.0,limit.r) + (newHeight - oldHeight));
	float wateroverhang = min(overhang, water);
	float wateradd = wateroverhang / max(1.0,erosion.r);


	// subtract any dropped water and evaporation
	newParticle.a = max(0.0,newParticle.a + wateradd - velocity.a); 
	//newParticle.a *= 0.99;

	// check to see if we did any hole-filling and kill particle
	//if (velocity.a > 0.0)
	//{
//		newParticle.ba = vec2(0.0);
	//}



	vec2 vel = vec2(cos(velocity.x),sin(velocity.x));
    //  move particle - maybe change to intersect with cell boundary
	newParticle.xy = particle.xy + vel * t * deltatime;

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

float hash2(vec2 co)
{
	return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

void main(void)
{
	vec4 particle = textureLod(particletex,texcoord,0);
	vec4 newParticle;

	newParticle = particle;
	//newParticle.a = max(0.0,particle.a - particleDeathRate);	
	bool die = false;

	//die= true;

	if ((newParticle.a < 0.001 && newParticle.z < 0.05) || die)
	{
		//newParticle.x = rand(particle.xy * 17.54 + rand(vec2(randSeed) + texcoord.yx * 97.3));
		//newParticle.y = rand(particle.yx * 93.11 + rand(vec2(randSeed + 0.073) + texcoord.xy * 17.3));
		newParticle.x = fract(198.0 * hash2(texcoord + vec2(randSeed)) * hash(particle.y + randSeed));
		newParticle.y = fract(336.0 * hash2(texcoord + vec2(randSeed)) * hash(particle.x + randSeed));
		newParticle.z = 0.0;
		newParticle.w = 0.5;  // TODO: make uniform (particle initial water amount)
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

//|CopyTerrain
#version 140
precision highp float;

uniform sampler2D terraintex;

in vec2 texcoord;
out vec4 out_Terrain;

void main(void)
{
	out_Terrain = textureLod(terraintex,texcoord,0);
}





