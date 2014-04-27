#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D paramTex;
uniform sampler2D detailTex;
uniform vec4 boxparam;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailScale;
uniform float detailTexScale;

in vec3 boxcoord;
in vec3 normal;
in vec3 binormal;
in vec3 tangent;
in vec2 detailpos_n;
in vec2 detailpos_s;
in vec2 detailpos_w;
in vec2 detailpos_e;

out vec4 out_NormalLargeScale;
out vec4 out_Normal;
out vec4 out_Param;


float t = 1.0 / 1024.0;
float sampleHeight(vec2 pos)
{
	return textureLod(detailTex,pos,0).r * detailTexScale;
}


vec3 getDetailNormal()
{
	float w = 2.0 / 32.0;

	float h1 = textureLod(detailTex,detailpos_n,0).r * detailTexScale;
	float h2 = textureLod(detailTex,detailpos_s,0).r * detailTexScale;
	float h3 = textureLod(detailTex,detailpos_w,0).r * detailTexScale;
	float h4 = textureLod(detailTex,detailpos_e,0).r * detailTexScale;

    return normalize(vec3(h4-h3,w,h2-h1));  // WAT
}


void main(void)
{
	vec2 texcoord = boxcoord.xz/boxparam.xy;

	// calculate normal of detail heightmap at detailpos
	mat3 nm = mat3(tangent,normal,binormal);
	vec3 dn = getDetailNormal();
	vec3 n = normalize(dn * nm);

	out_NormalLargeScale = vec4(normal.xyz * 0.5 + 0.5,1.0);
    out_Normal = vec4(n.xyz * 0.5 + 0.5,1.0);
	out_Param = texture(paramTex,texcoord);

}
