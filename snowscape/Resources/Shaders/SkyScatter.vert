#version 140

in vec3 vertex;
out vec2 sky2d;

void main() {

	gl_Position = vec4(vertex.xy,0.0,1.0);
	sky2d = vertex.xy;
}
