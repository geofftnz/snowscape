#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D paramTex;
uniform vec4 boxparam;

in vec3 boxcoord;
in vec3 normal;

out vec4 out_Normal;
out vec4 out_Param;

void main(void)
{
	vec2 texcoord = boxcoord.xz/boxparam.xy;
    out_Normal = vec4(normal.xyz * 0.5 + 0.5,1.0);
	out_Param = texture2D(paramTex,texcoord);

}
