#version 330 

layout (location = 0) in vec3 position;
layout (location = 1) in vec2 uv;
layout (location = 2) in vec3 vn;

uniform mat4 modelMatrix;
uniform mat4 lightSpaceMatrix;


out vec3 fragPos;
out vec2 fragTexCoord;
out vec3 fragNormal;

out vec3 cameraPos;
out vec4 fragPosLightSpace;

layout(std140) uniform Camera
{
	mat4 viewMatrix;
	mat4 projMatrix;
} camera;

void main(void)
{
	gl_Position = camera.projMatrix * camera.viewMatrix * modelMatrix * vec4(position, 1.0);

	fragPos = vec3(modelMatrix * vec4(position, 1.0));
	fragTexCoord = uv;

	fragNormal = mat3(modelMatrix)* vn;
	fragNormal = normalize(fragNormal);

	cameraPos = inverse(camera.viewMatrix)[3].xyz;
	
	fragPosLightSpace = lightSpaceMatrix * vec4(fragPos, 1.0);
}