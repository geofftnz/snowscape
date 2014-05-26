/*
	A collection of terrain generation to tile loaders 
*/

//|WaterGlobal
#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out vec4 out_Height;
uniform float waterHeightScale;

void main(void)
{
	vec4 h = textureLod(terraintex,texcoord,0);
	out_Height = vec4(h.r + h.g + h.b * waterHeightScale,0.0,0.0,0.0);
}

//|WaterTile
#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out vec4 out_Height;

uniform float waterHeightScale;

void main(void)
{
	vec4 t = textureLod(terraintex,texcoord,0);
	out_Height = vec4(t.r + t.g + t.b * waterHeightScale,0.0,0.0,0.0);
}

//|WaterParam
#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out vec4 out_Param;


void main(void)
{
	vec4 t = textureLod(terraintex,texcoord,0);

	vec4 p = vec4(0.0);

	p.r = clamp(t.g / 64.0,0.0,1.0);
	p.g = clamp(t.b,0.0,1.0);
	p.b = clamp(t.a * 32.0,0.0,1.0);

	out_Param = p;
}


//|SnowGlobal
#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out vec4 out_Height;

void main(void)
{
	vec4 h = textureLod(terraintex,texcoord,0);
	out_Height = vec4(h.r + h.g + h.b + h.a,0.0,0.0,0.0);
}

//|SnowTile
#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out vec4 out_Height;

void main(void)
{
	vec4 h = textureLod(terraintex,texcoord,0);
	out_Height = vec4(h.r + h.g + h.b + h.a,0.0,0.0,0.0);
}

//|SnowParam
#version 140
precision highp float;

uniform sampler2D terraintex;
in vec2 texcoord;
out vec4 out_Param;


void main(void)
{
	vec4 t = textureLod(terraintex,texcoord,0);

	vec4 p = vec4(0.0);

	p.r = clamp(t.g / 64.0,0.0,1.0);
	p.g = clamp(t.a,0.0,1.0);
	p.b = clamp(t.b,0.0,1.0);

	out_Param = p;
}
