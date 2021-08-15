//version 330 Core
out byte4 diffuse;
out vec3 normal;
out vec3 world_pos;
out float specular;
out int reflection_index;

in vec3 norm_data;
in vec3 frag_pos;

uniform vec3 objectColor;
uniform float specular_value;
uniform int reflectionData;

void main()
{
	diffuse = byte4(objectColor.x * 255.0f, objectColor.y * 255.0f, objectColor.z * 255.0f);
	//diffuse = byte4(norm_data.x * 127.5f + 127.5f, norm_data.y * 127.5f + 127.5f, norm_data.z * 127.5f + 127.5f);

	reflection_index = reflectionData;
	specular = specular_value;
	normal = norm_data;
	world_pos = frag_pos;
}
