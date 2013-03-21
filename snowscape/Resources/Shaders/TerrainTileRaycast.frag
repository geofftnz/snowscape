#version 140
precision highp float;
uniform mat4 model_matrix;
uniform sampler2D heightTex;
uniform sampler2D normalTex;
uniform sampler2D shadeTex;
uniform vec4 boxparam;  // dim of box
uniform vec3 eyePos;
uniform vec3 nEyePos;
in vec3 boxcoord;   // current box coord of back face, not normalized to 0-1
//in vec3 nboxcoord;

out vec4 out_Pos;
out vec4 out_Normal;
out vec4 out_Shade;
out vec4 out_Param;


float intersectBox ( vec3 rayo, vec3 rayd)
{
    vec3 omin = ( vec3(0.0,boxparam.z,0.0) - rayo ) / rayd;
    vec3 omax = ( boxparam.xwy - rayo ) / rayd;
    
    vec3 tmax = max ( omax, omin );
    vec3 tmin = min ( omax, omin );
    
    float t1 = min ( tmax.x, min ( tmax.y, tmax.z ) );
    float t2 = max ( max ( tmin.x, 0.0 ), max ( tmin.y, tmin.z ) );    
    
	return min(t1,t2);
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
	//vec3 nEyePos = (inverse(model_matrix) * vec4(eyePos,1.0)).xyz;

	vec3 boxEnter;
	vec3 boxExit = boxcoord;
	vec3 raydir = normalize(boxExit-nEyePos);
	
	// if eye is inside box, then boxenter=eye, else calculate intersection
	if (nEyePos.x >= 0.0 && nEyePos.y >= boxparam.z && nEyePos.z >= 0.0 &&
        nEyePos.x < boxparam.x && nEyePos.y <= boxparam.w && nEyePos.z < boxparam.y)
	{
		boxEnter = nEyePos;
	}
	else
	{
		boxEnter = nEyePos + intersectBox(nEyePos,raydir) * raydir;
	}

	//vec4 p = intersectHeightmap(boxEnter,raydir);

	//if (p.w > 0.6)
	//{
		vec2 texcoord = boxcoord.xz / boxparam.xy;
		//float h = texture2DLod(heightTex,texcoord,4).r;
		vec3 normal = normalize(texture2D(normalTex,texcoord).rgb - vec3(0.5,0.5,0.5));
		vec4 shade = texture2D(shadeTex,texcoord);

		out_Pos = vec4(worldPos.xyz,1.0);
		//out_Pos = vec4(p.xyz,1.0);
		//out_Normal = vec4(normal.xyz * 0.5 + 0.5,1.0);
		out_Normal = vec4(normal.xyz * 0.5 + 0.5,1.0);
		//out_Shade = vec4(shade.xyz,1.0);
		//out_Shade = vec4(nEyePos.xyz / 512.0,1.0);
		out_Shade = vec4(boxEnter.xyz * vec3(1.0/255.0,1.0/64.0,1.0/255.0),1.0);  // scale nboxcoord.y
		//out_Shade = vec4(0.1,t * 0.001 + 0.1,0.0,1.0);  // scale nboxcoord.y
		out_Param = vec4(boxExit.xyz * vec3(1.0/255.0,1.0/64.0,1.0/255.0),1.0);  // scale nboxcoord.y
		//out_Colour = vec4(boxcoord.xyz / 255.0,1.0);
	//}
	//else
	//{
		//discard;
	//}
//
}
