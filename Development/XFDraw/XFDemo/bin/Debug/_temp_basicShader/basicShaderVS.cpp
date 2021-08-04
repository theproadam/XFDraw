//version 330 Core
layout (location = 0) in vec3 pos;
layout (location = 1) in vec2 uv;

//uniform mat3 transform; //projection is done internally
out vec2 uv_data;
out vec3 frag_pos;

uniform vec3 cameraPos;
uniform mat3 cameraRot;

void main()
{
	gl_Position = cameraRot * (pos * 50.0f - cameraPos);
	uv_data = uv;
	frag_pos = pos * 50.0f;
}
//todo add inverse and transpose!

//todo test adding same name in and out