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
out vec3 normal;


vec3 getNormal(vec2 pos)
{
	float t = 1.0 / boxparam.x;

    float h1 = textureLod(heightTex,vec2(pos.x, pos.y - t),0).r;
	float h2 = textureLod(heightTex,vec2(pos.x, pos.y + t),0).r;
    float h3 = textureLod(heightTex,vec2(pos.x - t, pos.y),0).r;
	float h4 = textureLod(heightTex,vec2(pos.x + t, pos.y),0).r;

	return normalize(vec3(h3-h4,2.0,h1-h2));
}

// Takes vertices from a flat mesh and displaces them according to the heightmap texture.
// Generates the normal, boxcoord (coords within tile).
 
void main() {

	vec2 texcoord = in_boxcoord.xz;

	float h = textureLod(heightTex,texcoord,0).r;

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
}
