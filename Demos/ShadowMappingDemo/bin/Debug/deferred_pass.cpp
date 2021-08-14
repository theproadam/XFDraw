//version 330 Core
out byte4 FragColor;

in vec3 pos;
in vec3 norm;
in byte4 objectColor;
in float ssao;
in float spec_power;

uniform vec3 lightDir;
uniform sampler2D shadowMap;

uniform GLMatrix shadowProj;
uniform mat3 shadowRot;
uniform vec3 shadowPos;
uniform vec3 viewPos;

uniform float shadowBias;



uniform vec3 reflectPos1;
uniform vec3 reflectPos2;
uniform vec3 reflectPos3;
uniform vec3 reflectPos4;

float powf(float value, int count)
{
	if (count == 0) return 1.0f;
	else if (count == 1) return value;
	else if (count == 2) return value * value;
	else if (count == 4)
	{
		float v1 = value * value;
		return v1 * v1;
	}
	else if (count == 8)
	{
		float v1 = value * value;
		v1 *= v1;
		return v1 * v1;
	}
	else if (count == 16)
	{
		float v1 = value * value;
		v1 *= v1;
		v1 *= v1;
		return v1 * v1;
	}
	else if (count == 32)
	{
		float v1 = value * value;
		v1 *= v1;
		v1 *= v1;
		v1 *= v1;
		return v1 * v1;
	}
	else if (count == 64)
	{
		float v1 = value * value;
		v1 *= v1;
		v1 *= v1;
		v1 *= v1;
		v1 *= v1;
		return v1 * v1;
	}
	else if (count == 128)
	{
		float v1 = value * value;
		v1 *= v1;
		v1 *= v1;
		v1 *= v1;
		v1 *= v1;
		v1 *= v1;
		return v1 * v1;
	}
	else return 1.0f;
}

float max(float a, float b)
{
	if (a < b) return b;
	else return a;
}

float distSquared(vec3 a, vec3 b)
{
	float x = a.x - b.x;
	float y = a.y - b.y;
	float z = a.z - b.z;

	return x * x + y * y + z * z;
}

void main()
{
	if (*((int*)&(objectColor)) == 0)
		return;

//	FragColor = byte4(spec_power * 255, spec_power * 255, spec_power * 255);
//	return;

	const float ambientStrength = 0.1f;
	float specularStrength = spec_power;

	vec3 lightColor = vec3(0.8f, 0.8f, 0.8f);
	vec3 ambient = lightColor * ambientStrength;

	float diff = max(dot(norm, lightDir), 0.0);

	//Cubemap reflection
	if (true)
	{
		float dist1 = distSquared(pos, reflectPos1);
		float dist2 = distSquared(pos, reflectPos2);
		float dist3 = distSquared(pos, reflectPos3);
		float dist4 = distSquared(pos, reflectPos4);

		if (dist1 < dist2 && dist1 < dist3 && dist1 < dist4)
		{
			FragColor = byte4(255, 0, 0);
			return;
		}
		else if (dist2 < dist1 && dist2 < dist3 && dist2 < dist4)
		{
			FragColor = byte4(255, 255, 0);
			return;
		}
		else if (dist3 < dist1 && dist3 < dist2 && dist3 < dist4)
		{
			FragColor = byte4(255, 255, 255);
			return;
		}
		else
		{
			FragColor = byte4(0, 0, 0);
			return;
		}

	
		return;
	}


	float shadowResult = 1.0f;
	if (diff != 0)
	{
		//Calcualte Lightning
		vec3 uv = shadowProj * (shadowRot * (pos - shadowPos));
		float src = shadowProj.farZ - textureNEAREST<float>(shadowMap, int2(uv.x, uv.y));

		if (uv.z > src + shadowBias)
		{
			shadowResult = 0.0f;
		}
	}

	vec3 diffuse = lightColor * diff;
	vec3 viewDir = normalize(viewPos - pos);

	float spec = 0;

	if (false)
	{
		vec3 halfwayDir = normalize(lightDir + viewDir);// *0.5f;
		spec = powf(max(dot(norm, halfwayDir), 0.0f), 32);
	}
	else
	{
		vec3 reflectDir = reflect(-lightDir, norm);
		spec = powf(max(dot(viewDir, reflectDir), 0.0f), 32);
	}

	vec3 specular = lightColor * specularStrength * spec;

	vec3 result = vec3(objectColor.R, objectColor.G, objectColor.B) * (ambient + ((diffuse + specular) * shadowResult)) * (1.0f - ssao);

	if (result.x > 255) result.x = 255;
	if (result.y > 255) result.y = 255;
	if (result.z > 255) result.z = 255;

	FragColor = byte4(result.x, result.y, result.z);
}