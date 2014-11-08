#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D paramTex;
uniform vec4 boxparam;
uniform float patchSize;
uniform float scale;
uniform vec2 offset;
uniform float detailTexScale; // 0.1

in vec3 boxcoord;

out vec4 out_Param;
out vec4 out_Normal;
out vec4 out_NormalLargeScale;

void main(void)
{
	vec2 texcoord = boxcoord.xz/boxparam.xy;

    out_Normal = texture(normalTex,texcoord);
	out_NormalLargeScale = out_Normal;
	out_Param = texture(paramTex,texcoord);

}
