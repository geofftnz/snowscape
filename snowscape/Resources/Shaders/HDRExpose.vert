#version 140

in vec3 vertex;
out vec2 texcoord0;

void main() {

	gl_Position = vec4(vertex.xy,0.0,1.0);
	texcoord0 = (vertex.xy + vec2(1.0)) * 0.5;
}
