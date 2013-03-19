#version 140
precision highp float;
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D shadeTex;
uniform vec4 boxparam;
uniform vec3 eyePos;
uniform vec3 tileOffset;
in vec3 boxcoord;
out vec4 out_Pos;
out vec4 out_Normal;
out vec4 out_Shade;
out vec4 out_Param;


// get entry point of ray into box, in normalized box coordinates (xz 0-1, y min-max
vec3 getBoxEnter(vec3 rayo, vec3 rayd)
{
	float t = -1;
	vec3 intersection;

	// -X
	if (rayd.x > 0.0)
	{
		t = max(t,(0.0-rayo.x)/rayd.x);
	}
	// +X
	if (rayd.x < 0.0)
	{
		t = max(t,(1.0-rayo.x)/rayd.x);
	}
	// -Y
	if (rayd.y > 0.0)
	{
		t = max(t,(0-rayo.y)/rayd.y);
	}
	// +Y
	if (rayd.y < 0.0)
	{
		t = max(t,(1.0-rayo.y)/rayd.y);
	}
	// -Z
	if (rayd.z > 0.0)
	{
		t = max(t,(0-rayo.z)/rayd.z);
	}
	// +Z
	if (rayd.z < 0.0)
	{
		t = max(t,(1.0-rayo.z)/rayd.z);
	}
	return rayo + rayd * t;
}




void main(void)
{
    // generate world coordinate from offset, relative to eye
	vec3 worldPos = (boxcoord + tileOffset) - eyePos;
	vec3 nboxcoord = boxcoord.xyz / boxparam.x; // translate boxcoords into normalised space for raycasting

    
	vec2 texcoord = boxcoord.xz/256.0;
    float h = texture2DLod(heightTex,texcoord,4).r;
    vec3 normal = normalize(texture2D(normalTex,texcoord).rgb - vec3(0.5,0.5,0.5));
    vec4 shade = texture2D(shadeTex,texcoord);

    out_Pos = vec4(worldPos.xyz,1.0);
    out_Normal = vec4(normal.xyz * 0.5 + 0.5,1.0);
    out_Shade = vec4(shade.xyz,1.0);
    out_Param = vec4(nboxcoord.xyz,1.0);
    //out_Colour = vec4(boxcoord.xyz / 255.0,1.0);
}
