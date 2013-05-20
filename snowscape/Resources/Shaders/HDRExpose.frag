#version 140
precision highp float;

uniform sampler2D colTex;
uniform float exposure;

in vec2 texcoord0;
out vec4 out_Colour;



void main(void)
{
	
	vec3 col = texture(colTex,texcoord0).rgb;

	// apply exposure
	col.rgb = vec3(1.0) - exp(col.rgb * exposure);

	// gamma correct
	col = sqrt(col.rgb);

	// TODO: Anti-alias

	// output
	out_Colour = vec4(col,1.0);
}
