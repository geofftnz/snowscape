//|vs
#version 140

uniform mat4 inverse_projectionview_matrix;
uniform vec3 eyePos;

in vec3 vertex;

out vec4 eyeTarget;

void main() {
	gl_Position = vec4(vertex.xy,0.9999999,1.0);

	eyeTarget = inverse_projectionview_matrix * vec4(vertex.x, -vertex.y, 1.0, 1.0);
	eyeTarget /= eyeTarget.w;
}


//|fs

#version 140
precision highp float;

uniform vec3 eyePos;
uniform float iGlobalTime;

in vec4 eyeTarget;

out vec4 out_Col;



/*
	Pinching noise and basic raymarching setup from Octavio Good: https://www.shadertoy.com/view/4dSXDd
*/

// noise functions
float Hash1d(float u)
{
    return fract(sin(u)*143.9);	// scale this down to kill the jitters
}
float Hash2d(vec2 uv)
{
    float f = uv.x + uv.y * 37.0;
    return fract(sin(f)*104003.9);
}
float Hash3d(vec3 uv)
{
    float f = uv.x + uv.y * 37.0 + uv.z * 521.0;
    return fract(sin(f)*110003.9);
}
float mixP(float f0, float f1, float a)
{
    return mix(f0, f1, a*a*(3.0-2.0*a));
}
const vec2 zeroOne = vec2(0.0, 1.0);
float noise2d(vec2 uv)
{
    vec2 fr = fract(uv.xy);
    vec2 fl = floor(uv.xy);
    float h00 = Hash2d(fl);
    float h10 = Hash2d(fl + zeroOne.yx);
    float h01 = Hash2d(fl + zeroOne);
    float h11 = Hash2d(fl + zeroOne.yy);
    return mixP(mixP(h00, h10, fr.x), mixP(h01, h11, fr.x), fr.y);
}
float noise(vec3 uv)
{
    vec3 fr = fract(uv.xyz);
    vec3 fl = floor(uv.xyz);
    float h000 = Hash3d(fl);
    float h100 = Hash3d(fl + zeroOne.yxx);
    float h010 = Hash3d(fl + zeroOne.xyx);
    float h110 = Hash3d(fl + zeroOne.yyx);
    float h001 = Hash3d(fl + zeroOne.xxy);
    float h101 = Hash3d(fl + zeroOne.yxy);
    float h011 = Hash3d(fl + zeroOne.xyy);
    float h111 = Hash3d(fl + zeroOne.yyy);
    return mixP(
        mixP(mixP(h000, h100, fr.x),
             mixP(h010, h110, fr.x), fr.y),
        mixP(mixP(h001, h101, fr.x),
             mixP(h011, h111, fr.x), fr.y)
        , fr.z);
}

vec3 noise3(vec3 uv)
{
    return vec3(
        noise(uv),
        noise(uv.yzx + vec3(35.654,135.7,17.2)),
        noise(uv.zxy + vec3(19.7,39.7,117.7))
        );
}

float PI=3.14159265;


float sdSphere( vec3 p, float s )
{
	return length(p)-s;
}
float sdInvSphere( vec3 p, float s )
{
	return -sdSphere(p,s);
}
float sdqSphere( vec3 p, float ss )
{
	return dot(p,p)-ss;
}

vec2 opS( vec2 d1, vec2 d2 )
{
    //vec2 d3 = vec2(-d2.x,d2.y);
    //return max(-d2,d1);
    return (-d1.x > d2.x) ? vec2(-d1.x,d1.y) : d2;
}

vec2 opU( vec2 d1, vec2 d2 )
{
	return (d1.x<d2.x) ? d1 : d2;
}


vec2 distanceToScene(vec3 p)
{
  
    vec2 res = vec2(10000.0,0.0);
    
//    float t = 10000.0;//sdSphere(p - vec3(0.0), 0.1);
    
    
    for(float i=-0.5;i<5.0;i+=0.9)
    {
        res = opU(res,vec2(sdSphere(p - vec3(0.0,-0.05,i), 0.5),1.0));
        
        res = opU(res,vec2(sdSphere(p - vec3(-0.5,-0.05,i), 0.1),0.9));
        res = opU(res,vec2(sdSphere(p - vec3(0.5,-0.05,i), 0.1),0.9));
    }
    
    //t = sqrt(t);
    //if (camDist<5.0){
    
    float ns = 10.0, na = 0.02;
    float n = 0.0;
    n += noise(p*ns) * na; ns *= 2.0; na *= 0.5;
    n += noise(p*ns) * na; ns *= 2.0; na *= 0.5;
    n += noise(p*ns) * na; ns *= 2.0; na *= 0.5;
    n += noise(p*ns) * na; ns *= 2.0; na *= 0.5;
    
    res.x-=n;
    
        //t = t - noise(p*20.0) * 0.02;
        //t = t - noise(p*40.0) * 0.01;
        //t = t - noise(p*80.0) * 0.005;
    	//t = t - noise(p*200.0) * 0.001;
    //}
    
    //return t;
    return opS(res,vec2(sdSphere(p - vec3(0.0,0.0,-0.5), 50.0),0.0));
}

//float distanceToScene(vec3 p)
//{
//    return distanceToScene(p,0.0).x;
//}
    
vec3 getNormal(vec3 p, float dist)
{
    vec3 e = vec3(0.0025, 0, 0);
    return normalize(vec3(dist - distanceToScene(p - e.xyy).x,
                        dist - distanceToScene(p - e.yxy).x,
                        dist - distanceToScene(p - e.yyx).x));
}

float shadowMarch(vec3 p0, vec3 d)
{
    vec3 p;
    float s = 1.0;
    float t = 0.0;
    
    for(int i=0;i<16;i++)
    {
        p = p0 + d * t;
        float dist = distanceToScene(p).x;
        
        //s *= (1.0 - smoothstep(0.001,0.002,dist)*0.8);
       
        t += max(dist,0.1);
        //s *= 1.0 - clamp(abs(dist),0.0,1.0);
        
        //if (s<0.01) break;
        //if (abs(dist)<0.001) break;
            
        
        //t+=0.1;
        //if (s<0.001) break;
        //p = p0 + d * t * 0.05;
        //float dist= distanceToScene(p);
        //s *= (1.0 - smoothstep(0.0,0.001,dist)*0.5);
		
    }
    
    return s;
}

float softshadow( in vec3 ro, in vec3 rd, in float mint, in float tmax )
{
	float res = 1.0;
    float t = mint;
    for( int i=0; i<16; i++ )
    {
		float h = distanceToScene( ro + rd*t ).x;
        res = min( res, 8.0*h/t );
        t += clamp( h, 0.025, 0.20 );
        if( h<0.001 || t>tmax ) break;
    }
    return clamp( res, 0.0, 1.0 );

}

vec3 tempToColour(float t)
{
    return vec3(
        smoothstep(500.0,2000.0,t) * 0.95,
        smoothstep(900.0,5000.0,t),
        smoothstep(2000.0,10000.0,t)
       )*4.0;
}

float OrenNayar(float roughness, float albedo, vec3 rd, vec3 nor, vec3 light)
{
    // oren-nayar
    //float roughness = 0.5;
    //float albedo = 1.0;
    float roughness2 = roughness*roughness;
    float onA = 1.0 - 0.5 * (roughness / (roughness+0.57));
    float onB = 0.45 * (roughness / (roughness+0.09));

    float ndotl = dot(nor,light);
    float ndotv = dot(nor,-rd);

    float ai = acos(ndotl);
    float ar = acos(ndotv);

    float onAlpha = max(ai,ar);
    float onBeta = min(ai,ar);
    float onGamma = dot(-rd - nor * dot(-rd,nor),light - nor * dot(light,nor)); // ?

    return (albedo / 3.1415927) * max(0.0,dot(nor,light)) * (onA + (onB * max(0.0,onGamma) * sin(onAlpha) * tan(onBeta)));
}

void main()
{
    
    vec3 lightV = normalize(vec3(-1.0,0.8,0.0));
    vec3 lightC = vec3(0.85,0.9,1.0) * 0.05;
    vec3 camLightC = vec3(0.95,0.92,0.8) * 0.5;
    
 //   vec2 muv = iMouse.xy / iResolution.xy * 2.0 - 1.0;
  
	//vec3 camPos = vec3(0.0,0.0,1.5+sin(iGlobalTime * 0.1) * 2.0);
 //   //vec3 camFacing;
	//vec3 camUp=vec3(0.0,1.0,0.0);
	//vec3 camLookat=vec3(muv.x,muv.y,camPos.z + 1.0);
    
	//vec2 uv = fragCoord.xy/iResolution.xy * 2.0 - 1.0;

	//// Camera setup.
	//vec3 camVec=normalize(camLookat - camPos);
	//vec3 sideNorm=normalize(cross(camUp, camVec));
	//vec3 upNorm=cross(camVec, sideNorm);
	//vec3 worldFacing=(camPos + camVec);
	//vec3 worldPix = worldFacing + uv.x * sideNorm * (iResolution.x/iResolution.y) + uv.y * upNorm;
	//vec3 relVec = normalize(worldPix - camPos);


	vec3 camPos = eyePos;
	vec3 relVec = normalize(eyeTarget.xyz - eyePos);
    
    
    // material parameters
    vec3 emmissive = vec3(0.0);
    vec3 albedo = vec3(0.0);
    float specexp = 30.0;
    float spec = 0.0;
    float roughness = 0.0;
    
    
    // Raymarch
    float t = 0.0;
    vec2 dist = vec2(0.1,0.0);
    float distMax = 20.0;
    vec3 p = vec3(0.0);
    
    for(int i=0;i<64;i++)
    {
        if ((dist.x > distMax) || (abs(dist.x) < 0.001)) break;
        
        p = camPos + relVec * t*0.95;
        dist = distanceToScene(p);
        t += dist.x;//*0.9999;
    }
    
    vec3 nor = getNormal(p,dist.x);

    // material
    if (dist.y>0.95)
    {
        vec3 n3 = noise3(p * 400.0);
        
        albedo = vec3(0.3) + n3 * vec3(0.05,0.07,0.1);
        roughness = 0.9;
        
        nor = normalize(nor + (n3 - 0.5) * 0.2);
        
        float sn = noise(p*vec3(0.5,9.0,9.0)) + noise(p*30.0) * 0.05;
        //spec = (smoothstep(0.9,0.92,sn) * (1.0-smoothstep(0.95,0.97,sn))) * smoothstep(0.5,0.7,noise(p*3.0));
        spec = smoothstep(0.99,0.992,sn);
        
        spec += smoothstep(0.7,0.95,noise(p * 15.0)) * 0.05;
        
        albedo *= (1.0-spec*0.5); 
        roughness *= (1.0-spec);
    }
    else if (dist.y > 0.85)
    {
        float temperature = 
            500.0 + 
            smoothstep(0.0,1.0,noise(p*3.0)) * 300.0 + 
            //smoothstep(0.6,1.0,noise(p*40.0)) * 950.0 + 
            smoothstep(0.7,1.0,noise(p*40.0)) * 3000.0;
        temperature *= abs(sin(iGlobalTime * 0.05));
        emmissive = tempToColour(temperature);
        albedo = vec3(0.3);
    }
    
    vec3 col = vec3(0.0);
    //vec3 diffuse = vec3(0.2);
    
    // hit
    if (abs(dist.x) < 0.1)
    {
        
        // diffuse
        col = albedo * clamp(dot(nor,lightV),0.0,1.0) * lightC;
        
        // camera light
        vec3 camLightV = (camPos-p + vec3(0.1,-0.22,-0.1));
        float camLightPower = 1.0 / dot(camLightV,camLightV);
        camLightV = normalize(camLightV);
        //camLightPower *= shadowMarch(p,-camLightV);
        //camLightPower *= (softshadow(p,camLightV,0.02, 0.5));
        
        float diffuse = OrenNayar(1.0, 1.0, relVec, nor, camLightV);
        
        col += albedo * clamp(diffuse,0.0,1.0) * camLightC * camLightPower;
        
        // specular
        vec3 refl = reflect(relVec,nor);
        col += spec * pow(clamp(dot(refl,camLightV),0.0,1.0),specexp) * camLightC * camLightPower;
        
        // emmissive
        col += emmissive;
        
        //col = nor * 0.5 + 0.5;
    }

    float fog = 1.0 - 1.0 / exp(  t * 0.9);
    
    
    col = mix(col,vec3(0.0),fog);
    fog = max(0.0,fog-0.2);
    col = mix(col,vec3(0.6,0.8,1.0)*0.1,fog*fog);

    
    // gamma
    col = pow(col,vec3(0.4545));

    out_Col = vec4(col,1.0);

}


