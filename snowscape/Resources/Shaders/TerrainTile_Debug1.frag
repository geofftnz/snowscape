#version 140
precision highp float;

in vec3 boxcoord;
out vec4 out_Colour;

void main(void)
{
    out_Colour = vec4(boxcoord.xyz / 255.0,1.0);
}
