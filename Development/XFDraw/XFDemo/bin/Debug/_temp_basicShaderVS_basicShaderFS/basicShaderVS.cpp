//version 330 Core
in vec3 pos;
in vec2 uv_data;

out vec3 FragPos;
out vec3 Normal;

uniform mat4 transform; //projection is done internally

void main()
{
	fragPos = pos;
	gl_Position = vec3(transform * vec4(pos, 1.0f));
	uv = uv_data;
}
//todo add inverse and transpose!