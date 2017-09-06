<<<<<<< HEAD
#version 330
=======
#version 330 core
>>>>>>> master

in vec3 position;

uniform mat4 projMatrix;
uniform mat4 viewMatrix;

void main(void)
{
<<<<<<< HEAD
	gl_Position =  projMatrix * viewMatrix * vec4(position, 1.0);
=======
	gl_Position = projMatrix * viewMatrix * vec4(position, 1.0);
>>>>>>> master
}