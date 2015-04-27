//|vs
#version 140

uniform mat4 inverse_projectionview_matrix;
uniform vec3 eyePos;

in vec3 vertex;

out vec4 eyeTarget;

void main() {
	gl_Position = vec4(vertex.xy,0.9999999,1.0);

	eyeTarget = inverse_projectionview_matrix * vec4(vertex.x, vertex.y, -0.9, 1.0);
	eyeTarget /= eyeTarget.w;
}


//|fs

#version 140
precision highp float;

uniform vec3 eyePos;
in vec4 eyeTarget;

out vec4 out_Col;

void main(void)
{
	out_Col = vec4(normalize(eyeTarget.xyz - eyePos)*0.5+0.5,1.0);
}
