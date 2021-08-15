//version 330 Core
layout(location = 0) in vec3 pos;
layout(location = 1) in vec2 uv;

out vec2 uv_data;
out vec3 frag_pos;

uniform vec3 cameraPos;
uniform mat3 cameraRot;

uniform vec3 objectPos;

void main()
{
	vec3 world_pos = pos + objectPos;

	gl_Position = cameraRot * (world_pos - cameraPos);

	frag_pos = world_pos;
	uv_data = uv;
}