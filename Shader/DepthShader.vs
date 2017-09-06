#version 330

layout (location = 1) in vec3 position;

uniform mat4 lightSpaceMatrix;
uniform mat4 modelMatrix;

void main(void)
{
	gl_Position =  lightSpaceMatrix * modelMatrix * vec4(position, 1.0);
}