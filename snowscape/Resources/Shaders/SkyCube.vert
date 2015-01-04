#version 140

uniform mat4 inverse_projectionview_matrix;
uniform vec3 eyePos;

in vec3 vertex;

out vec3 rayDir;

void main() {
	gl_Position = vec4(vertex.xy,0.9999999,1.0);

	vec4 eyeTarget = inverse_projectionview_matrix * vec4(vertex.x, vertex.y, -0.9, 1.0);
	eyeTarget /= eyeTarget.w;

	rayDir = normalize(eyeTarget.xyz - eyePos);

}
