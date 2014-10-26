//|VS
#version 140
 
uniform sampler2D heightTex;


uniform mat4 projection_matrix;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform vec4 boxparam;
uniform vec3 eyePos;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;

in vec3 vertex;
in vec3 in_boxcoord;

out vec3 boxcoord;
//out vec3 worldpos;
//out vec3 normal;
//out vec3 binormal;
//out vec3 tangent;
//out vec2 detailpos;


float t = 1.0 / boxparam.x;


float getHeight(vec2 pos)
{
	return textureLod(heightTex,pos,0).r;
}

float sampleHeight(vec2 pos)
{
	float c = getHeight(pos);
	return c;
}

//vec3 getNormal(vec2 pos)
//{
	//return texture(normalTex,pos).rgb * 2.0 - vec3(1.0);
//}

void main() {

	vec3 b = in_boxcoord;
	b.xz *= scale;
	b.xz += offset;
	vec2 texcoord = b.xz;
//
	//highp vec2 pos = mod(texcoord,boxparam.x);
	//detailpos = pos * 32.0;
//
	float h = sampleHeight(texcoord);
	//normal = getNormal(texcoord);
//
	//vec3 t1 = normalize(cross(normal,vec3(0.0,0.0,-1.0)));
	//binormal = normalize(cross(t1,normal));
	//tangent = normalize(cross(normal,binormal));
//
	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h + v.y * 1.0f;

    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(v, 1.0);

	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;
	//worldpos = (model_matrix * vec4(b,1.0)).xyz - eyePos;

}

//|FS

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

    out_Normal = vec4(0.0,1.0,0.0,0.0);
	out_NormalLargeScale = out_Normal;
	out_Param = vec4(0.0,1.0,0.0,0.0);

}
