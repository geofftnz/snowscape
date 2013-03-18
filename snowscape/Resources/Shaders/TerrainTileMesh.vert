#version 140
 
uniform sampler2D heightTex;

uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
uniform vec4 boxparam;
in vec3 vertex;
in vec3 in_boxcoord;

out vec3 boxcoord;
 
void main() {

	vec2 texcoord = in_boxcoord.xz;

	float h = texture2D(heightTex,texcoord).r;
    //normal = normalize(texture2D(normalTex,texcoord).rgb - vec3(0.5,0.5,0.5));
    //shade = texture2D(shadeTex,texcoord);

	vec3 v = vertex;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = h;

    gl_Position = projection_matrix * modelview_matrix * vec4(v, 1.0);

	vec3 b = in_boxcoord;
	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = h;
    
    boxcoord = b;
}
