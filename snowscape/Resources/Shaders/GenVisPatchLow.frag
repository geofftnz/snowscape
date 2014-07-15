#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D paramTex;
uniform vec4 boxparam;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailTexScale; // 0.1

in vec3 boxcoord;
//in vec3 normal;
//in vec3 binormal;
//in vec3 tangent;
//in vec2 detailpos;

out vec4 out_Param;
out vec4 out_Normal;
out vec4 out_NormalLargeScale;

/*
float t = 1.0 / 1024.0;
float sampleHeight(vec2 pos)
{
	return textureLod(detailTex,pos,0).r * detailTexScale;
}

vec3 getDetailNormal(vec2 pos)
{
	float w = 2.0 / 32.0;

    float h1 = sampleHeight(vec2(pos.x, pos.y - t));
    float h2 = sampleHeight(vec2(pos.x, pos.y + t));
    float h3 = sampleHeight(vec2(pos.x - t, pos.y));
    float h4 = sampleHeight(vec2(pos.x + t, pos.y));
    return normalize(vec3(h4-h3,w,h2-h1));  // WAT
}
*/
void main(void)
{
	vec2 texcoord = boxcoord.xz/boxparam.xy;

	//mat3 nm = mat3(tangent,normal,binormal);
	//vec3 dn = getDetailNormal(detailpos);		// calculate detail normal using heights from detail texture.
	//vec3 n = normalize(dn * nm);

    out_Normal = texture(normalTex,texcoord);
	out_NormalLargeScale = out_Normal;
	out_Param = texture(paramTex,texcoord);

}
