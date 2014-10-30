//|VS
#version 140
 
uniform mat4 projection_matrix;
uniform mat4 model_matrix;
uniform mat4 view_matrix;

in vec3 vertex;
in vec4 colour;

out vec4 out_col;

void main() {

    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(vertex, 1.0);
	out_col = colour;

}

//|FS

#version 140
precision highp float;

in vec4 out_col;

void main(void)
{
	gl_Fragcolor = out_col;
}
