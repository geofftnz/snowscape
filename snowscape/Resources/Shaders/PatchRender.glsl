//|LowVertex
#version 140
 
uniform sampler2D heightTex;

uniform mat4 transform_matrix;
uniform vec4 boxparam;
uniform vec3 eyePos;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;

in vec3 vertex;
in vec3 in_boxcoord;

out vec3 boxcoord;
out vec2 texcoord;

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

void main() {

	vec3 b = in_boxcoord;
	b.xz *= scale;
	b.xz += offset;
	vec2 texcoord = b.xz;

	float h = sampleHeight(texcoord);

	vec3 v = vertex;
	v.xz *= scale;
	v.xz += offset;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h + v.y * 1.0f;

    gl_Position = transform_matrix * vec4(v, 1.0);

	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;

	texcoord = boxcoord.xz/boxparam.xy;

}

//|LowFragment

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
in vec2 texcoord;

out vec4 out_AlbedoShadow;
out vec4 out_Shading;
out vec4 out_NormalAO;


void main(void)
{
    out_Normal = texture(normalTex,texcoord);
	out_NormalLargeScale = out_Normal;
	out_Param = texture(paramTex,texcoord);

}
