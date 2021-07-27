//version 330 Core
out byte4 FragColor;

in vec3 FragPos;
in vec3 Normal;

struct Material {
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
	float shininess;
};

struct Light {
	vec3 position;

	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
};

uniform vec3 viewPos;
uniform Material material;
uniform Light light;

void main()
{
	vec3 result = vec3(0.5f, 0.5f, 0.5);
	FragColor = byte4(result.x * 255, result.y * 255, result.z * 255);
}
