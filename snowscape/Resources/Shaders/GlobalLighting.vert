#version 140
 
in vec3 vertex;
in vec2 in_texcoord0;
out vec2 texcoord0;
 
void main() {
    gl_Position = vec4(vertex.x * 2.0 - 1.0,1.0-vertex.y * 2.0,0.0,1.0); 
    texcoord0 = in_texcoord0;
}
