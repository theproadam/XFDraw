//version 330 Core
out byte4 FragColor;

in vec3 norm_data;
in vec3 frag_pos;

uniform vec3 lightDir;
uniform vec3 objectColor;

inline float max(float a, float b)
{
	if (a < b) return b;
	else return a;
}


void main()
{
	const float ambientStrength = 0.1f;

	vec3 lightColor = vec3(0.8f, 0.8f, 0.8f);
	vec3 ambient = lightColor * ambientStrength;

	float diff = max(dot(norm_data, lightDir), 0.0);
	vec3 diffuse = lightColor * diff;

	vec3 result = objectColor * (ambient + diffuse) * 255.0f;

	if (result.x > 255) result.x = 255;
	if (result.y > 255) result.y = 255;
	if (result.z > 255) result.z = 255;

	FragColor = byte4(result.x, result.y, result.z);
}
