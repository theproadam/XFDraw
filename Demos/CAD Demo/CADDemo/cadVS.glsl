layout (location = 0) in vec3 vertex_data;

uniform vec3 cameraPos;
uniform mat3 cameraRot;

void main()
{
	gl_Position = cameraRot * (vertex_data - cameraPos);

	int val = gl_InstanceID;
}