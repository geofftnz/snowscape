#version 140
 
uniform mat4 projection_matrix;
uniform mat4 model_matrix;
uniform mat4 view_matrix;
uniform vec4 boxparam;
in vec3 vertex;
in vec3 in_boxcoord;
out vec3 boxcoord;
//out vec3 nboxcoord;
 
void main() {

	vec3 v = vertex;
	v.x *= boxparam.x;
	v.z *= boxparam.y;
	v.y = (v.y * (boxparam.w - boxparam.z)) + boxparam.z;

    gl_Position = projection_matrix * view_matrix * model_matrix  * vec4(v, 1.0);

	// normalised box coords (x,z: [0,1], y: world)
	//nboxcoord = in_boxcoord;
	//nboxcoord.y = ((nboxcoord.y * (boxparam.w - boxparam.z)) + boxparam.z);

	vec3 b = in_boxcoord;
	b.x *= boxparam.x;
	b.z *= boxparam.y;
	b.y = (b.y * (boxparam.w - boxparam.z)) + boxparam.z;
    
    boxcoord = b;
}
