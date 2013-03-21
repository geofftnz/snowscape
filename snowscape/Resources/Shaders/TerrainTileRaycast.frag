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

vec4 intersectHeightmap(vec3 boxEnter, vec3 posRayDir)
{
	vec4 p = vec4(0.0);

	vec3 texEntry;
	vec3 texExit;
	vec3 texHit;
	float height= 0.0f;
	float t,tx,tz,qx,qz;

	float umul=1.0f, uofs=0.0f, vmul=1.0f, vofs=0.0f;	// texture coordinate flipping

	highp int TEXLOG2 = int(log2(boxparam.x));

	highp int level = TEXLOG2-1;  // replace with log2(texdim)-1

	float p2l = exp2(level); // 2^level

	// normalize boxcoords to 0-1
	//boxEnter.xz /= boxparam.xy;
	//posRayDir.xz /= boxparam.xy;

	if (posRayDir.x < 0.0f) // dx negative, invert x on texture sample
	{
		posRayDir.x = -posRayDir.x;
		boxEnter.x = boxparam.x - boxEnter.x;
		umul=-1.0f;
		uofs=boxparam.x;
	}
	if (posRayDir.z < 0.0f) // dz negative, invert z on texture sample
	{
		posRayDir.z = -posRayDir.z;
		boxEnter.z = boxparam.y - boxEnter.z;
		vmul=-1.0f;
		vofs=boxparam.y;
	}

	texEntry = boxEnter;

	float n = 0.0f;
	vec2 texScale = vec2(1.0,1.0) / boxparam.xy;

	while ( texEntry.x < boxparam.x && texEntry.z < boxparam.y && p.w < 0.5f ) 
	{
		n = n + 0.01;

		height = texture2DLod(heightTex, vec2(texEntry.x+uofs, texEntry.z+vofs) * texScale, level).r; // grab height at point for mip level
			
		qx = (floor(texEntry.x / p2l) + 1.0f) * p2l;		
		qz = (floor(texEntry.z / p2l) + 1.0f) * p2l;  // quantize texcoords for level
			
		tx = (qx - texEntry.x) / posRayDir.x; 
		tz = (qz - texEntry.z) / posRayDir.z; // compute intersections
			
		t = min(tx,tz); // closest intersection

		texExit = texEntry + posRayDir * t; // exit point
		texExit = 
			vec3(
				(t == tx)? ((floor(texEntry.x / p2l)+1.0)* p2l) : texExit.x, 
				texExit.y, 
				(t == tz)?((floor(texEntry.z / p2l)+1.0)* p2l) : texExit.z
				);  // correct for rounding errors
			
		if (  ( (posRayDir.y < 0.0f) ? texExit.y : texEntry.y) <= height) // intersection, hit point = texEntry
		{
			// actual hit location
			p.xyz = (posRayDir.y < 0.0f) ? texEntry + posRayDir * max((height - texEntry.y) / posRayDir.y,0.0f) : texEntry;

			if (level < 1)  // at actual intersection
			{
				p.w = 0.61 + n;
			}
			else // still walking through the mipmaps
			{
				texEntry = p.xyz;  // advance ray to hit point
				level--;  // drop level
				p2l = exp2(level);  // update quantization factor
			}
		}
		else // no intersection
		{
			texEntry = texExit;  // move ray to exit point

			vec2 texExit2 = mod(floor(texExit.xz / p2l),vec2(2.0,2.0));

			level = 
				(t == tx) 
				? min(level + 1 - int(texExit2.x) ,TEXLOG2-1) 
				: min(level + 1 - int(texExit2.y) ,TEXLOG2-1); // go up a level if we reach the edge of our current 2x2 block

			p2l = exp2(level);  // update quantization factor
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

	vec4 p = intersectHeightmap(boxEnter,raydir);

	if (p.w > 0.6)
	{
		vec2 texcoord = p.xz / boxparam.xy;

		vec3 normal = normalize(texture2D(normalTex,texcoord).rgb - vec3(0.5,0.5,0.5));
		vec4 shade = texture2D(shadeTex,texcoord);

		// position in world, relative to eye
		out_Pos = vec4(p.xyz- eyePos,p.w);

		// normal at intersection
		out_Normal = vec4(normal.xyz * 0.5 + 0.5,1.0);

		// shade at intersection
		out_Shade = vec4(shade.xyz,1.0);

		// aux params at intersection
		out_Param = vec4(0.5,0.5,0.5,1.0);

	}

}
