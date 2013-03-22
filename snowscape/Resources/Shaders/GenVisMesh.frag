#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D paramTex;
uniform vec4 boxparam;

in vec3 boxcoord;
in vec3 worldpos;
in vec3 normal;

out vec4 out_Pos;
out vec4 out_Normal;
out vec4 out_Param;

void main(void)
{
	vec2 texcoord = boxcoord.xz/boxparam.xy;
    //float h = texture2D(heightTex,texcoord).r;
    vec3 n = normalize(normal);

    out_Pos = vec4(worldpos.xyz,1.0);
    out_Normal = vec4(n.xyz * 0.5 + 0.5,1.0);
	out_Param = texture2D(paramTex,texcoord);
}
