//version 330 Core
out byte4 FragColor;

in vec3 pos;
in vec3 norm;
in byte4 objectColor;


uniform vec3 lightDir;
uniform sampler2D shadowMap;

uniform GLMatrix shadowProj;
uniform mat3 shadowRot;
uniform vec3 shadowPos;

uniform float shadowBias;

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

	if (diff != 0)
	{
		//Calcualte Lightning
		vec3 uv = shadowProj * (shadowRot * (pos - shadowPos));
		float src = shadowProj.farZ - textureNEAREST<float>(shadowMap, int2(uv.x, uv.y));

		if (uv.z > src + shadowBias)
		{
			diff = 0;
		}
	}

	vec3 diffuse = lightColor * diff;

	vec3 result = vec3(objectColor.R, objectColor.G, objectColor.B) * (ambient + diffuse);

	FragColor = byte4(result.x, result.y, result.z);
}