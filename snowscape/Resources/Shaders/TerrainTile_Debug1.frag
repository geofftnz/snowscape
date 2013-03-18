﻿#version 140
precision highp float;

uniform sampler2D heightTex;

uniform vec3 eyePos;
uniform vec3 tileOffset;

in vec3 boxcoord;

out vec4 out_Pos;
out vec4 out_Normal;
out vec4 out_Shade;
out vec4 out_Param;

void main(void)
{

	// generate world coordinate from offset, relative to eye
	vec3 worldPos = (boxcoord + tileOffset) - eyePos;

	float h = texture2D(heightTex,boxcoord.xz/256.0).r;

	out_Pos = vec4(worldPos.xzy,1.0);
	out_Normal = vec4(h / 255.0,0.0,0.0,1.0);
	out_Shade = vec4(0.5,1.0,0.5,1.0);
	out_Param = vec4(0.5,0.5,1.0,1.0);

    //out_Colour = vec4(boxcoord.xyz / 255.0,1.0);
}
