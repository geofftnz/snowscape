#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D paramTex;
uniform sampler2D detailTex;
uniform vec4 boxparam;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;

in vec3 boxcoord;
in vec3 worldpos;
in vec3 normal;
in vec3 binormal;
in vec3 tangent;
in vec2 detailpos;

out vec4 out_Pos;
out vec4 out_Normal;
out vec4 out_Param;


float t = 1.0 / 1024.0;
float sampleHeight(vec2 pos)
{
	return textureLod(detailTex,pos,0).r * 0.1;
}

vec3 getDetailNormal(vec2 pos)
{
	//pos *= 32.0;
	//vec3 ofs = vec3(-t,0.0,t);

	float w = 2.0 / 32.0;

    //float h1 = sampleHeight(pos + ofs.yx); // 0,-1
    //float h2 = sampleHeight(pos + ofs.yz);  // 0 1
    //float h3 = sampleHeight(pos + ofs.xy); // -1 0
    //float h4 = sampleHeight(pos + ofs.zy); // 1 0
    float h1 = sampleHeight(vec2(pos.x, pos.y - t));
    float h2 = sampleHeight(vec2(pos.x, pos.y + t));
    float h3 = sampleHeight(vec2(pos.x - t, pos.y));
    float h4 = sampleHeight(vec2(pos.x + t, pos.y));
    return normalize(vec3(h4-h3,w,h2-h1));  // WAT
}

void main(void)
{
	vec2 texcoord = boxcoord.xz/boxparam.xy;
    //float h = texture2D(heightTex,texcoord).r;
    //vec3 n = normal; //normalize(normal);

	mat3 nm = mat3(tangent,normal,binormal);

	//vec3 dn = vec3(0.0,1.0,0.0);
	vec3 dn = getDetailNormal(detailpos);

	//vec3 n = getDetailNormal(detailpos);
	vec3 n = normalize(dn * nm);


    out_Pos = vec4(worldpos.xyz,1.0);
    out_Normal = vec4(n.xyz * 0.5 + 0.5,1.0);
	out_Param = texture(paramTex,texcoord);

}
