#version 140

in vec3 vertex;
out vec2 texcoord;

void main() {

	gl_Position = vec4(vertex.xy,0.0,1.0);
	texcoord = vertex.xy * 0.5 + 0.5;
}
