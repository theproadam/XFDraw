layout (location = 0) in vec3 vertex_data;
layout (location = 1) in vec3 norm_data;

out vec3 normals;

uniform vec3 cameraPos;
uniform mat3 cameraRot;
uniform vec3 objectPos;

uniform mat3 objectRot;


uniform float zoomMod;

void main()
{
	vec3 pos = cameraRot * (((objectRot * vertex_data) * 0.01f * zoomMod + objectPos) - cameraPos);
	normals = norm_data;

	gl_Position = pos;
}