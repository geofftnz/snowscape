#version 330
precision highp float;

uniform vec3 eyePos;
noperspective in vec4 eyeTarget;
//noperspective in vec3 eyePos2;

out vec4 out_Pos;

void main(void)
{
	out_Pos = vec4(normalize(eyeTarget.xyz - eyePos) * vec3(1.0,-1.0,1.0),0.1);
	//out_Pos = vec4(eyeTarget.xyz,0.1);

	//gl_FragDepth = -0.5;
}
