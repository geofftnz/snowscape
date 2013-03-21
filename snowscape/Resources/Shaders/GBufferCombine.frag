#version 140
precision highp float;

uniform sampler2D posTex;
uniform sampler2D normalTex;
uniform sampler2D shadeTex;
uniform sampler2D paramTex;

uniform vec3 eyePos;
uniform vec3 sunVector;

in vec2 texcoord0;
out vec4 out_Colour;


void main(void)
{
	vec2 p = texcoord0.xy;
	vec4 c = vec4(0.0,0.0,0.0,1.0);

	vec4 normalTex = texture2D(normalTex,p);
	vec4 posTex = texture2D(posTex,p);

	vec3 pos = posTex.xyz + eyePos;
	vec3 normal = normalize(normalTex.xyz - 0.5);

	float diffuse = dot(normal, sunVector) * 0.5 + 0.5;

	c.rgb = vec3(1.0) * diffuse;

	/*
	vec2 p = texcoord0.xy * 2.0;
	// split screen into 4
	if (p.x < 1.0)
	{
		if (p.y < 1.0)
		{
			vec3 pos = texture2D(posTex,p).xyz + eyePos;
			c = vec4(pos.xyz/512.0,0.0) + vec4(0.5,0.5,0.5,1.0);
		}
		else
		{
			c = texture2D(normalTex,p-vec2(0.0,1.0));
		}
	}
	else
	{
		if (p.y < 1.0)
		{
			c = texture2D(shadeTex,p-vec2(1.0,0.0));
		}
		else
		{
			c = texture2D(paramTex,p-vec2(1.0,1.0));
		}
	}*/

    out_Colour = vec4(c.rgb,1.0);
}
