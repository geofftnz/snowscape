#version 140
precision highp float;

uniform sampler2D heightTex;
in vec3 boxcoord;
out vec4 out_Colour;

void main(void)
{
	float h = texture2D(heightTex,boxcoord.xz/256.0).r;

	out_Colour = vec4(h / 255.0,0.0,0.0,1.0);

    //out_Colour = vec4(boxcoord.xyz / 255.0,1.0);
}
