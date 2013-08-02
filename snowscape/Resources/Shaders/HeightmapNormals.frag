#version 140
precision highp float;

uniform sampler2D heightmap;
uniform float texsize;

in vec2 texcoord;

out vec4 out_Normal;

float t = 1.0 / texsize;


float sampleHeight(vec2 pos)
{
	return texture(heightmap,pos).r;
}

vec3 getNormal(vec2 pos)
{

    float h1 = sampleHeight(vec2(pos.x, pos.y - t));
	float h2 = sampleHeight(vec2(pos.x, pos.y + t));
    float h3 = sampleHeight(vec2(pos.x - t, pos.y));
	float h4 = sampleHeight(vec2(pos.x + t, pos.y));

	return normalize(vec3(h3-h4,2.0,h1-h2));
}

void main(void)
{
	out_Normal = vec4(getNormal(texcoord) * 0.5 + vec3(0.5),0.0);
}
