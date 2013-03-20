#version 140
precision highp float;
uniform mat4 model_matrix;
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D shadeTex;
uniform vec4 boxparam;  // dim of box
uniform vec3 eyePos;
in vec3 boxcoord;   // current box coord of back face, not normalized to 0-1
//in vec3 nboxcoord;

out vec4 out_Pos;
out vec4 out_Normal;
out vec4 out_Shade;
out vec4 out_Param;


// get entry point of ray into box, in normalized box coordinates (xz 0-1, y min-max
float intersectBox(vec3 rayo, vec3 rayd)
{
	float t = -1;

	// -X
	if (rayd.x > 0.0)
	{
		t = max(t,(0.0-rayo.x)/rayd.x);
	}
	// +X
	if (rayd.x < 0.0)
	{
		t = max(t,(boxparam.x-rayo.x)/rayd.x);
	}
	// -Y
	if (rayd.y > 0.0)
	{
		t = max(t,(boxparam.z-rayo.y)/rayd.y);
	}
	// +Y
	if (rayd.y < 0.0)
	{
		t = max(t,(boxparam.w-rayo.y)/rayd.y);
	}
	// -Z
	if (rayd.z > 0.0)
	{
		t = max(t,(0-rayo.z)/rayd.z);
	}
	// +Z
	if (rayd.z < 0.0)
	{
		t = max(t,(boxparam.y-rayo.z)/rayd.z);
	}
	return t;
}

#define TEXDIM 256
#define TEXLOG2 8

// TODO: convert to full-scale coords.

vec4 intersectHeightmap(vec3 boxEnter, vec3 posRayDir)
{
	vec4 p = vec4(0.0);

	vec3 texEntry;
	vec3 texExit;
	vec3 texHit;
	float height= 0.0f;
	float t,tx,tz,qx,qz,qf;

	float umul=1.0f, uofs=0.0f, vmul=1.0f, vofs=0.0f;	// texture coordinate flipping

	int level = TEXLOG2-1;  // replace with log2(texdim)-1
	qf = pow(2.0f,TEXLOG2-level); // quantization factor

	// normalize boxcoords to 0-1
	//boxEnter.xz /= boxparam.xy;
	//posRayDir.xz /= boxparam.xy;

	if (posRayDir.x < 0.0f) // dx negative, invert x on texture sample
	{
		posRayDir.x = -posRayDir.x;
		boxEnter.x = 1.0f - boxEnter.x;
		umul=-1.0f;
		uofs=1.0f;
	}
	if (posRayDir.z < 0.0f) // dz negative, invert z on texture sample
	{
		posRayDir.z = -posRayDir.z;
		boxEnter.z = 1.0f - boxEnter.z;
		vmul=-1.0f;
		vofs=1.0f;
	}

	texEntry = boxEnter;

	float n = 0.0f;

	while ( texEntry.x < 1.0f && texEntry.z < 1.0f && p.w < 0.5f ) 
	{
		n = n + 0.01;

		height = texture2DLod(heightTex, vec2(texEntry.x+uofs, texEntry.z+vofs), level).r; // grab height at point for mip level
			
		qx = (floor(texEntry.x * qf) + 1.0f) / qf;		
		qz = (floor(texEntry.z * qf) + 1.0f) / qf;  // quantize texcoords for level
			
		tx = (qx - texEntry.x) / posRayDir.x; 
		tz = (qz - texEntry.z) / posRayDir.z; // compute intersections
			
		t = min(tx,tz); // closest intersection

		texExit = texEntry + posRayDir * t; // exit point
		texExit = vec3((t == tx)?((floor(texEntry.x*qf)+1.0f)/qf):texExit.x, texExit.y, (t == tz)?((floor(texEntry.z*qf)+1.0f)/qf):texExit.z);  // correct for rounding errors
			
		if (  ( (posRayDir.y < 0.0f) ? texExit.y : texEntry.y)    <= height) // intersection, hit point = texEntry
		{
			// actual hit location
			p.xyz = (posRayDir.y < 0.0f) ? texEntry + posRayDir * max((height - texEntry.y) / posRayDir.y,0.0f) : texEntry;

			if (level < 1)  // at actual intersection
			{
				p.w = 0.61f + n;
			}
			else // still walking through the mipmaps
			{
				texEntry = p.xyz;  // advance ray to hit point
				level--;  // drop level
				qf = pow(2.0f,TEXLOG2-level);  // update quantization factor
			}
		}
		else // no intersection
		{
			texEntry = texExit;  // move ray to exit point
			level = (t == tx) ?  min(level+1-int(mod(floor(texExit.x*qf),2.0f)) ,TEXLOG2-1) : min(level+1-int(mod(floor(texExit.z*qf),2.0f)) ,TEXLOG2-1); // go up a level if we reach the edge of our current 2x2 block
			qf = pow(2.0f,TEXLOG2-level); // update quantization factor
		}
	}  // end of while loop

	p.x = umul * p.x + uofs;
	p.z = vmul * p.z + vofs;

    return p;
}


void main(void)
{
    // generate world coordinate from offset, relative to eye
	vec3 worldPos = (model_matrix * vec4(boxcoord,1.0)).xyz - eyePos;
	
	// translate eyepos into normalized box coord space.
	vec3 neyepos = (inverse(model_matrix) * vec4(eyePos,1.0)).xyz;

	vec3 boxEnter;
	vec3 boxExit = boxcoord;
	vec3 raydir = normalize(boxExit-neyepos);
	
	// if eye is inside box, then boxenter=eye, else calculate intersection
	if (neyepos.x >= 0.0 && neyepos.y >= boxparam.z && neyepos.z >= 0.0 &&
        neyepos.x < boxparam.x && neyepos.y <= boxparam.w && neyepos.z < boxparam.y)
	{
		boxEnter = neyepos;
	}
	else
	{
		boxEnter = neyepos + intersectBox(neyepos,raydir) * raydir;
	}

	vec4 p = intersectHeightmap(boxEnter,raydir);

	if (p.w > 0.6)
	{
		vec2 texcoord = boxcoord.xz / boxparam.xy;
		//float h = texture2DLod(heightTex,texcoord,4).r;
		vec3 normal = normalize(texture2D(normalTex,texcoord).rgb - vec3(0.5,0.5,0.5));
		vec4 shade = texture2D(shadeTex,texcoord);

		//out_Pos = vec4(worldPos.xyz,1.0);
		out_Pos = vec4(p.xyz,1.0);
		//out_Normal = vec4(normal.xyz * 0.5 + 0.5,1.0);
		out_Normal = vec4(normal.xyz * 0.5 + 0.5,1.0);
		//out_Shade = vec4(shade.xyz,1.0);
		//out_Shade = vec4(neyepos.xyz / 512.0,1.0);
		out_Shade = vec4(boxEnter.xyz * vec3(1.0/255.0,1.0/64.0,1.0/255.0),1.0);  // scale nboxcoord.y
		//out_Shade = vec4(0.1,t * 0.001 + 0.1,0.0,1.0);  // scale nboxcoord.y
		out_Param = vec4(boxExit.xyz * vec3(1.0/255.0,1.0/64.0,1.0/255.0),1.0);  // scale nboxcoord.y
		//out_Colour = vec4(boxcoord.xyz / 255.0,1.0);
	}
	else
	{
		discard;
	}

}
