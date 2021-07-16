//#version 330 core (haha jk you dont need this)
in vec3 aPos;
in vec3 aNormal;

out vec3 fragPos;
out vec3 normal;

uniform vec3 cameraPos;
uniform vec3 cameraRot;

void main()
{
	normal = aNormal;
	gl_Position = aPos;
}