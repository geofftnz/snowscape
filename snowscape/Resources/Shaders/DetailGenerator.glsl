//|vert
#version 140
precision highp float;
in vec3 vertex;
out vec2 texcoord;

uniform float invtexsize;
uniform vec2 position;
const float scale = 16.0;
const float invscale = 1.0 / 16.0;

void main() {

	gl_Position = vec4(vertex.xy,0.0,1.0);
	texcoord = (vertex.xy * 0.5) * invscale + position * invtexsize;
}

//|frag
#version 140
precision highp float;

uniform sampler2D heighttex;
uniform sampler2D paramtex;
uniform float invtexsize;


in vec2 texcoord;

out float out_Height;
out vec4 out_Normal;
out vec4 out_Param;

float t = invtexsize;

float sampleHeight(vec2 pos)
{
	return textureLod(heighttex,pos,0).r;
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
	out_Height = sampleHeight(texcoord);
	out_Normal = vec4(getNormal(texcoord),1.0);
	out_Param = textureLod(paramtex,texcoord,0);
}
