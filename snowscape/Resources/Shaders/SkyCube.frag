#version 140
precision highp float;

uniform samplerCube skyCube;
in vec3 rayDir;

out vec4 out_Colour;
out vec4 out_Normal;

void main(void)
{
	//out_Normal = vec4(normalize(eyeTarget.xyz - eyePos)*0.5+0.5,0.1);

	out_Colour = vec4(textureCube(skyCube,rayDir).rgb,0.1);  // alpha is temp for sky
	out_Normal = vec4(rayDir,0.1);
	gl_FragDepth = 0.999999;
}
