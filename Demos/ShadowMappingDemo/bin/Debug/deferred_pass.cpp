//version 330 Core
out byte4 FragColor;

in vec3 pos;
in vec3 norm;
in byte4 objectColor;


uniform vec3 lightDir;


float max(float a, float b)
{
	if (a < b) return b;
	else return a;
}

void main()
{
	if (*((int*)&(objectColor)) == 0)
		return;

	const float ambientStrength = 0.1f;

	vec3 lightColor = vec3(0.8f, 0.8f, 0.8f);
	vec3 ambient = lightColor * ambientStrength;

	float diff = max(dot(norm, lightDir), 0.0);
	vec3 diffuse = lightColor * diff;

	vec3 result = vec3(objectColor.R, objectColor.G, objectColor.B) * (ambient + diffuse);

	FragColor = byte4(result.x, result.y, result.z);
}