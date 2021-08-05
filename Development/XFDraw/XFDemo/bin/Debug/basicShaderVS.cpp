//version 330 Core
layout (location = 0) in vec3 pos;
layout (location = 1) in vec3 norm;
layout (location = 2) in vec2 uv;
layout (location = 3) in vec3 tangent;
layout (location = 4) in vec3 Bitangent;

//uniform mat3 transform; //projection is done internally
out vec2 uv_data;
out vec3 normal;
out vec3 frag_pos;
out vec3 TangentViewPos;
out vec3 TangentFragPos;


uniform vec3 cameraPos;
uniform mat3 cameraRot;


void main()
{
	gl_Position = cameraRot * (pos * 50.0f - cameraPos);
	uv_data = uv;
	vec3 FragPos = pos * 50.0f;

	vec3 T = tangent;
	vec3 B = Bitangent;
	vec3 N = norm;

	mat3 TBN = transpose(mat3(T, B, N));
	TangentViewPos = TBN * cameraPos;
	TangentFragPos = TBN * FragPos;

	normal = norm;
	frag_pos = (pos * 50.0f);
}
//todo add inverse
//todo test adding same name in and out