#version 140
 
uniform sampler2D heightTex;

uniform mat4 projection_matrix;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform vec4 boxparam;
uniform vec3 eyePos;

in vec3 vertex;
in vec3 in_boxcoord;
in vec4 in_normal;
in vec4 in_shade;
in vec4 in_param;

out vec3 boxcoord;
out vec3 worldpos;
out vec4 normal;
out vec4 shade;
out vec4 param;
 
void main() {

	vec2 texcoord = in_boxcoord.xz;
	
	float h = texture2D(heightTex,texcoord).r;

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
