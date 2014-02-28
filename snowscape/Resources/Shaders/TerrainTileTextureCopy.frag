#version 140
precision highp float;

uniform sampler2D terrainTex;
in vec2 texcoord;

out float out_Height;
out vec4 out_Param;

void main(void)
{
	vec4 t = texture2D(terrainTex,texcoord);
	out_Height = t.r + t.g + t.b;
	//out_Height = 100.0 * sin(texcoord.x * 20.0) * cos(texcoord.y*20.0); //t.r + t.g + t.b;
	out_Param = vec4(t.gba,0.0);
}
