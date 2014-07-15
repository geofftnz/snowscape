#version 140
 
uniform sampler2D heightTex;
//uniform sampler2D normalTex;

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
