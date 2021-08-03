//version 330 Core
layout(location = 0) in vec3 pos;
layout(location = 1) in vec3 norm;

out vec3 norm_data;
out vec3 frag_pos;

uniform vec3 cameraPos;
uniform mat3 cameraRot;

void main()
{
	gl_Position = cameraRot * (pos - cameraPos);
	norm_data = norm;
	frag_pos = pos;

}