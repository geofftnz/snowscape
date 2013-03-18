#version 140
precision highp float;

uniform sampler2D posTex;
uniform sampler2D normalTex;
uniform sampler2D shadeTex;
uniform sampler2D paramTex;

in vec2 texcoord0;
out vec4 out_Colour;


void main(void)
{
	vec4 c = texture2D(posTex,texcoord0.xy);

    out_Colour = vec4(c.rgb,1.0);
}
