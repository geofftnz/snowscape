#version 140
precision highp float;

uniform sampler2D heightTex;
uniform sampler2D paramTex;
in vec2 texcoord;

out float out_Height;
out vec4 out_Param;

void main(void)
{
	out_Height = texture2D(heightTex,texcoord).r;
	out_Param = texture2D(paramTex,texcoord);
}
