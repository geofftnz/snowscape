#version 140
 
uniform sampler2D heightTex;

uniform mat4 projection_matrix;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform vec4 boxparam;
uniform vec3 eyePos;

in vec3 vertex;
in vec3 in_boxcoord;

out vec3 boxcoord;
out vec3 worldpos;
out vec3 normal;


vec3 getNormal(vec2 pos)
{
	float t = 1.0 / boxparam.x;

    float h1 = texture2D(heightTex,vec2(pos.x, pos.y - t)).r;
	float h2 = texture2D(heightTex,vec2(pos.x, pos.y + t)).r;
    float h3 = texture2D(heightTex,vec2(pos.x - t, pos.y)).r;
	float h4 = texture2D(heightTex,vec2(pos.x + t, pos.y)).r;

    return normalize(vec3(h4-h3,h2-h1,2.0));
}

 
void main() {

	vec2 texcoord = in_boxcoord.xz;
	
	float h = texture2D(heightTex,texcoord).r;

	normal = getNormal(texcoord);

	vec3 v = vertex;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h;

    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(v, 1.0);

	vec3 b = in_boxcoord;
	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;
	worldpos = (model_matrix * vec4(b,1.0)).xyz - eyePos;

}
