#version 330

uniform mat4 inverse_projectionview_matrix;
uniform vec3 eyePos;

layout(location = 0) in vec3 vertex;
//layout(location = 1) in vec3 vcoord;

noperspective out vec4 eyeTarget;
//noperspective out vec3 eyePos2;

void main() {

	//vec4 v = vec4(vertex.xy;

	gl_Position = vec4(vertex.xy,0.9999999,1.0);

	//eyeTarget = vec4(vcoord,1.0);

	eyeTarget = inverse_projectionview_matrix * vec4(vertex.x, vertex.y, -0.9, 1.0);
	eyeTarget /= eyeTarget.w;

	//eyePos2 = eyePos;
	//eyePos2 = (view_matrix * vec4(eyePos,1.0)).xyz;
}
