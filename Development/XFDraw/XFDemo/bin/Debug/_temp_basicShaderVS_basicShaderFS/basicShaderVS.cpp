//version 330 Core
layout (location = 0) in vec3 pos;
layout (location = 1) in vec3 norm;

out vec3 FragPos;
out vec3 Normal;

uniform mat4 transform; //projection is done internally
uniform vec3 someData;

void main()
{
	FragPos = pos;
	gl_Position = vec3(transform * vec4(pos, 1.0f));
	Normal = norm;
}
//todo add inverse and transpose!

//todo test adding same name in and out