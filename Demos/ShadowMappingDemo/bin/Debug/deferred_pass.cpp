//version 330 Core
out byte4 FragColor;

in vec3 pos;
in vec3 norm;
in byte4 objectColor;
in float ssao;
in float spec_power;
in int reflection_index;

uniform vec3 lightDir;
uniform sampler2D shadowMap;

uniform GLMatrix shadowProj;
uniform mat3 shadowRot;
uniform vec3 shadowPos;
uniform vec3 viewPos;

uniform float shadowBias;


uniform samplerCube reflect1;
uniform samplerCube reflect2;
uniform samplerCube reflect3;
uniform samplerCube reflect4;


uniform vec3 reflectPos1;
uniform vec3 reflectPos2;
uniform vec3 reflectPos3;
uniform vec3 reflectPos4;

inline float powf(float value, int count)
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

inline float max(float a, float b)
{
	if (a < b) return b;
	else return a;
}


inline byte4 textureNEARESTTest(samplerCube inputCubemap, vec3 dir)
{
	//FACE_INDEX_VALUE
	// RIGHT = INDEX 0
	// LEFT = INDEX 1
	// TOP = INDEX 2
	// BOTTOM = INDEX 3
	// FRONT = INDEX 4
	// BACK = INDEX 5

	int face;
	vec2 uv = Cubemap_UVFace(dir, face);

	if (face == 1) {
		face = 0;
	}
	else if (face == 0) { 
		face = 1; 
	}

	uv.x = 1.0f - uv.x;

	if (uv.x > 1) uv.x = 1;
	else if (uv.x < 0) uv.x = 0;

	if (uv.y > 1) uv.y = 1;
	else if (uv.y < 0) uv.y = 0;


	int X = uv.x * (inputCubemap.width - 1);
	int Y = uv.y * (inputCubemap.height - 1);



	if (face == 0)
	{
		return inputCubemap.right[X + Y * inputCubemap.width];
	}
	else if (face == 1)
	{
		return inputCubemap.left[X + Y * inputCubemap.width];
	}
	else if (face == 2)
	{
		return inputCubemap.top[X + Y * inputCubemap.width];
	}
	else if (face == 3)
	{
		return inputCubemap.bottom[X + Y * inputCubemap.width];
	}
	else if (face == 4)
	{
		return inputCubemap.front[X + Y * inputCubemap.width];
	}
	else if (face == 5)
	{
		return inputCubemap.back[X + Y * inputCubemap.width];
	}

}


void main()
{
	if (*((int*)&(objectColor)) == 0)
		return;

//	FragColor = byte4(spec_power * 255, spec_power * 255, spec_power * 255);
//	return;

	const float ambientStrength = 0.2f;
	float specularStrength = spec_power;

	vec3 lightColor = vec3(0.8f, 0.8f, 0.8f);
	vec3 ambient = lightColor * ambientStrength;

	float diff = max(dot(norm, lightDir), 0.0);

	vec3 objColr = vec3(objectColor.R, objectColor.G, objectColor.B) * 0.00392156862745f;

	//Cubemap reflection
	if (reflection_index != 0)
	{
		byte4 result;

		vec3 I = (pos - viewPos);		
		vec3 R = reflect(I, norm);

		//not sure why the reflections arent aligning :(
		//because of this im forced to use a custom texture cubemap sampler function!

		if (reflection_index == 1)
		{
			result = textureNEARESTTest(reflect1, R);
		}
		else if (reflection_index == 2)
		{
			result = textureNEARESTTest(reflect2, R);
		}
		else if (reflection_index == 3)
		{
			result = textureNEARESTTest(reflect3, R);
		}
		else
		{
			result = textureNEARESTTest(reflect4, R);
		}

		//FragColor = result;
		//return;
	
		objColr = objColr * (1.0f - specularStrength) + result.xyz() * specularStrength;
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

	vec3 result = objColr * (ambient + ((diffuse + specular) * shadowResult)) * (1.0f - ssao) * 255.0f;

	if (result.x > 255) result.x = 255;
	if (result.y > 255) result.y = 255;
	if (result.z > 255) result.z = 255;

	FragColor = byte4(result.x, result.y, result.z);
}