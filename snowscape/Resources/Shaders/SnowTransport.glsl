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
//uniform sampler2D densitytex;

uniform float texsize;
uniform vec2 windvelocity;
uniform float lowpass;
uniform float terrainfactor;
uniform float noisefactor;
//uniform float densityfactor;
uniform float randseed;
uniform float deltatime;
uniform float airceiling;
uniform float airmass;
uniform float windlowpass;

in vec2 texcoord;
out vec4 out_Velocity;

float t = 1.0 / texsize;
vec3 tt = vec3(-t,0,t);

#include "noise.glsl"

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

//vec2 getDensityGradient(vec2 pos)
//{
    //float n = texture(densitytex,pos+tt.yx).r;
	//float s = texture(densitytex,pos+tt.yz).r;
    //float w = texture(densitytex,pos+tt.xy).r;
	//float e = texture(densitytex,pos+tt.zy).r;
	//return vec2(e-w,n-s);
//}
//
vec2 getRandomVelocity(vec2 pos)
{
	vec2 r;
	r.x = (rand(pos.xy + vec2(randseed))-0.5);
	r.y = (rand(pos.xy + vec2(randseed * 3.19 + 7.36))-0.5);
	return r;
}

void main(void)
{
	vec4 particle = texture(particletex,texcoord);
	vec4 prevvel = texture(velocitytex,texcoord);
	
	// TODO: Interpolate
	float terrainheight = sampleHeight(particle.xy);

	// calculate new base height of air column and write to vel.b
	// don't let the air column be lower than the terrain
	//float airheight = max(terrainheight, particle.b-(aircolumnfallrate*deltatime));
	

	vec3 n = getNormal(particle.xy);
	//vec2 d = getDensityGradient(particle.xy);

	vec2 newvel = 
		windvelocity + 
		n.xy * terrainfactor + 
		//d.xy * densityfactor + 
		getRandomVelocity(particle.xy) * noisefactor;

	// wind speed is related to how much we're squashing the air column.
	float newwindspeed = airmass / max(1.0,(airceiling - particle.b));

	out_Velocity = vec4(
						mix(newvel,prevvel.xy,lowpass),
						terrainheight - particle.b,  // difference between air column base and terrain
						mix(newwindspeed,prevvel.a,windlowpass)  // windspeed
						);
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
uniform float erosionheightthreshold;
uniform float depositRateConst;

in vec2 texcoord;
in vec2 particlecoord;

out vec4 out_Erosion;

void main(void)
{
    //   Calculate particle potential as (new carrying capacity - carrying amount).
    //   Writes R:1 G:potential B:deposit - blend as add
	vec4 particle = textureLod(particletex,particlecoord,0);
	vec4 velocity = textureLod(velocitytex,particlecoord,0);

	//float carryingCapacity = velocity.a * carryingspeedcoefficent;
	float carrying = particle.a;
	float heightdiff = velocity.b; // height of base of air column above terrain.

	// erosion based on wind speed, if air column base is at/near ground
	float erosionPotential = velocity.a * erosionRate * deltatime * (1.0-smoothstep(erosionheightthreshold*0.5,erosionheightthreshold+0.001,heightdiff));
	
	// deposit 
	float depositAmount = 
		min(
			carrying,
			(carrying * depositRateConst) +
				(1.0 + smoothstep(erosionheightthreshold*0.5,erosionheightthreshold+0.001,heightdiff)) * depositRate
			) * deltatime;

	out_Erosion = vec4(1.0, erosionPotential, depositAmount, carrying);
}


//|UpdateTerrain
#version 140
precision highp float;

uniform sampler2D terraintex;
uniform sampler2D erosiontex;

uniform float packedSnowErosionFactor;

in vec2 texcoord;
out vec4 out_Terrain;

//  Adds deposit amount E.b to powder L0.a
//  Subtracts material from powder, then packed.

void main(void)
{
	vec4 terrain = textureLod(terraintex,texcoord,0);
	vec4 erosion = textureLod(erosiontex,texcoord,0);

	float packedsnow = terrain.b;
	float powder = terrain.a;

	powder += erosion.b;  // add deposit amount to powder, make available for erosion

	// calculate erosion from powder - lesser of potential and amount of powder
	float powdererode = min(powder, erosion.g);
	float packedsnowerode = min(packedsnow, max(erosion.g - powdererode,0.0) * packedSnowErosionFactor);

	out_Terrain = vec4(
		terrain.rg,
		packedsnow - packedsnowerode, 
		powder - powdererode
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
uniform float erosionheightthreshold;
uniform float depositRateConst;
uniform float texsize;
uniform float aircolumnfallrate;
uniform float packedSnowErosionFactor;


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


	float carrying = particle.a;
	float heightdiff = velocity.b; // height of base of air column above terrain.

	// erosion based on wind speed, if air column base is at/near ground
	float erosionPotential = velocity.a * erosionRate * deltatime * (1.0-smoothstep(erosionheightthreshold*0.5,erosionheightthreshold+0.001,heightdiff));
	
	// deposit 
	float depositAmount = 
		min(
			carrying,
			(carrying * depositRateConst) +
				(1.0 + smoothstep(erosionheightthreshold*0.5,erosionheightthreshold+0.001,heightdiff)) * depositRate
			) * deltatime;

	// reduce our carrying amount by the amount we're depositing
	newParticle.a = max(carrying - depositAmount,0.0);

	// calculate total material being eroded from the terrain at our position.
	float packedsnow = terrain.b;
	float powder = terrain.a;
	powder += erosion.b;  // add deposit amount to powder, make available for erosion
	// calculate erosion from powder - lesser of potential and amount of powder
	float powdererode = min(powder, erosion.g);
	float packedsnowerode = min(packedsnow, max(erosion.g - powdererode,0.0) * packedSnowErosionFactor);
	float totaleroded = powdererode + packedsnowerode;

    //  Add material carried based on particle share of total V0.b / E(P0.rg).g -> P1.b
	float particleerode = totaleroded * (erosionPotential / erosion.g);
	newParticle.a += particleerode;


    //  move particle 
	newParticle.xy = particle.xy + normalize(velocity.xy) * t * deltatime;

	// keep in range
	newParticle.xy = mod(newParticle.xy + vec2(1.0),vec2(1.0));

	// calculate new base height of air column and write to particle.z
	// don't let the air column be lower than the terrain
	newParticle.z = max(dot(terrain,vec4(1.0)), particle.b-(aircolumnfallrate*deltatime));


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

