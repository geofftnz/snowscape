//|ComputeVelocity

#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D particletex;
uniform sampler2D velocitytex;
uniform float texsize;
uniform float vdecay;
uniform float vadd;
uniform float speedCarryingCoefficient;

in vec2 texcoord;

out vec4 out_Velocity;

float t = 1.0 / texsize;
float diag = 0.707;


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
	vec2 v = fall.xy;

	//  Takes velocity from V1.rg, applies acceleration, writes to V0.rg
	vec2 newVelocity = prevvel.xy * vdecay + v * vadd;

	//  Calculates new carrying capacity and writes to V0.b 
	float speed = length(newVelocity);
	float carryingCapacity = speed * speedCarryingCoefficient;

	// TODO: make sure carrying capacity is sensible
	
	out_Velocity = vec4(newVelocity.xy,carryingCapacity,0);
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

	float erosionPotential = max(velocity.b - particle.b,0.0) * erosionRate * deltatime;
	float depositAmount = max(particle.b - velocity.b,0.0) * depositRate * deltatime;

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
		terrain.r - harderode, 
		terrain.g - softerode,
		terrain.b * waterLowpass + erosion.r * waterDepthFactor * (1.0-waterLowpass),
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
out vec4 out_Particles;

float t = 1.0 / texsize;

void main(void)
{
	vec4 particle = textureLod(particletex,texcoord,0);
	vec4 velocity = textureLod(velocitytex,texcoord,0);
	vec2 terraincoord = particle.xy;
	vec4 erosion = textureLod(erosiontex,terraincoord,0);
	vec4 terrain = textureLod(terraintex,terraincoord,0);

	vec4 newParticle = particle;

    //  Replicate particle potential calc from Step 2 to get deposit/erode potentials.
	float erosionPotential = max(velocity.b - particle.b,0.0) * erosionRate * deltatime;
	float depositAmount = max(particle.b - velocity.b,0.0) * depositRate * deltatime;

    //  Subtract deposit amount from carrying amount P0.b, write to P1.b
	newParticle.b = max(particle.b - depositAmount,0.0);

    //  Apply same calculation as step 3 to determine how much soft/hard is being eroded from L0(P0.rg).
	float hard = terrain.r;
	float soft = terrain.g;

	soft += erosion.b;  // add deposit amount to soft, make available for erosion

	// calculate erosion from soft - lesser of potential and amount of soft material
	float softerode = min(soft, erosion.g);
	float harderode = max(erosion.g - softerode,0.0) * hardErosionFactor;
	float totaleroded = softerode + harderode;

    //  Add material carried based on particle share of total V0.b / E(P0.rg).g -> P1.b
	float particleerode = totaleroded * (max(velocity.b - particle.b,0.0) * erosionRate * deltatime) / erosion.g;
	newParticle.b += particleerode;


    //  TODO: Calculate new particle position by intersecting ray P0.rg->V0.rg against 
    //    cell boundaries. Add small offset to avoid boundary problems. Writes to P1.rg
    //  If death flag indicates particle recycle, init particle at random position.
	newParticle.xy = particle.xy + normalize(velocity.xy) * t;

	out_Particles = newParticle;

}

//|CopyParticles
#version 140
precision highp float;

uniform sampler2D particletex;

in vec2 texcoord;
out vec4 out_Particles;

void main(void)
{
	out_Particles = textureLod(particletex,texcoord,0);
}


