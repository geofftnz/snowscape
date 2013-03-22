#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D shadeTex;
uniform sampler2D paramTex;
uniform vec4 boxparam;

in vec3 boxcoord;
in vec3 worldpos;

out vec4 out_Pos;
out vec4 out_Normal;
out vec4 out_Shade;
out vec4 out_Param;
void main(void)
{
	vec2 texcoord = boxcoord.xz/boxparam.xy;
    float h = texture2D(heightTex,texcoord).r;
    vec3 normal = normalize(texture2D(normalTex,texcoord).rgb - vec3(0.5,0.5,0.5));

    out_Pos = vec4(worldpos.xyz,1.0);
    out_Normal = vec4(normal.xyz * 0.5 + 0.5,1.0);
    out_Shade = texture2D(shadeTex,texcoord);
	out_Param = texture2D(paramTex,texcoord);
}
