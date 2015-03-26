#version 140

uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
in vec3 vertex;
in vec2 in_texcoord0;
noperspective out vec2 texcoord0;
 
void main() {
    gl_Position = projection_matrix * modelview_matrix * vec4(vertex, 1.0);
    texcoord0 = in_texcoord0;
}
