#version 140
precision highp float;

uniform sampler2D heightTex;
in vec3 boxcoord;

out vec4 out_Pos;
out vec4 out_Normal;
out vec4 out_Shade;
out vec4 out_Param;

void main(void)
{
	float h = texture2D(heightTex,boxcoord.xz/256.0).r;

	out_Pos = vec4(h / 255.0,0.0,0.0,1.0);
	out_Normal = vec4(1.0,0.5,0.5,1.0);
	out_Shade = vec4(0.5,1.0,0.5,1.0);
	out_Param = vec4(0.5,0.5,1.0,1.0);

    //out_Colour = vec4(boxcoord.xyz / 255.0,1.0);
}
