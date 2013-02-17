#version 140
 
uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
uniform vec2 tex0_offset;
uniform float tex0_scale;
in vec3 vertex;
in vec2 in_texcoord0;
out vec2 texcoord0;
 
void main() {
    gl_Position = projection_matrix * modelview_matrix * vec4(vertex, 1.0);
    vec2 ofs = tex0_offset;
    texcoord0 = ((in_texcoord0 - vec2(0.5,0.5)) * tex0_scale + vec2(0.5,0.5)) + ofs;
}
