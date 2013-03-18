#version 140
 
uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
uniform vec4 boxparam;
in vec3 vertex;
in vec3 in_boxcoord;
out vec3 boxcoord;
 
void main() {

	vertex.x *= boxparam.x;
	vertex.z *= boxparam.y;
	vertex.y = (vertex.y * (boxparam.w - boxparam.z)) + boxparam.z;

    gl_Position = projection_matrix * modelview_matrix * vec4(vertex, 1.0);

	in_boxcoord.x *= boxparam.x;
	in_boxcoord.z *= boxparam.y;
	in_boxcoord.y = (in_boxcoord.y * (boxparam.w - boxparam.z)) + boxparam.z;
    
    boxcoord = in_boxcoord;
}
