//version 330 Core
out byte4 FragColor;

in vec2 uv_data;
in vec3 normal;
in vec3 frag_pos;
in vec3 TangentViewPos;
in vec3 TangentFragPos;

uniform sampler2D myTexture;
uniform vec2 textureSize;
uniform vec3 camera_Pos;
uniform float heightScale;
uniform sampler2D depthMap;
uniform float waterLevel;
uniform samplerCube skybox;

float semiRandom1(float x)
{
	return 200.0f * x * (x - 0.2f) * (x - 0.5f) * (x - 0.8f) * (x - 1.0f);
}

float semiRandom(float x)
{
	return 200.0f * x * (x - 0.33f) * (x - 0.66f) * (x - 1.0f);
}

float semiRandom2(float x)
{
	return 1000.0f * x * (x - 0.16f) * (x - 0.33f) * (x - 0.66f) * (x - 0.83f) * (x - 1.0f);
}

void main()
{

	vec3 viewDir = normalize(TangentViewPos - TangentFragPos);
	//vec3 viewDir = normalize(frag_pos - camera_Pos);
	byte4 reslt = texture(depthMap, vec2(uv_data.x * textureSize.x, uv_data.y * textureSize.y));

	float height = 1.0f - (float)reslt.R * 0.00392156862745f;

	vec2 texCoords = uv_data - vec2(viewDir) * (height * heightScale);

	if (texCoords.x > 1.0 || texCoords.y > 1.0 || texCoords.x < 0.0 || texCoords.y < 0.0)
	{
		return;
	}

	// WaterLevel->


	byte4 coord_sample = texture(depthMap, vec2(texCoords.x * textureSize.x, texCoords.y * textureSize.y));
	float waterCount = 1.0f - (float)coord_sample.R * 0.00392156862745f;

	if (waterCount >= waterLevel)
	{
		//FragColor = byte4(70, 70, 255);

		byte4 waterCol = byte4(51, 135, 255);

		vec3 I = (frag_pos - camera_Pos);
		vec3 R = reflect(I, normal);

		const float mult = 6.94444E-7;

		R.x += 8.0f * semiRandom(texCoords.x);
		R.y += 8.0f * semiRandom(texCoords.y);
	//	R.z += 4.0f * semiRandom(texCoords.x * texCoords.y);


		byte4 sky_col = textureNEAREST(skybox, R);
		sky_col.R = sky_col.R * 0.25f + waterCol.R * 0.75f;
		sky_col.G = sky_col.G * 0.25f + waterCol.G * 0.75f;
		sky_col.B = sky_col.B * 0.25f + waterCol.B * 0.75f;



		FragColor = sky_col;

		return;
	}

	FragColor = texture(myTexture, vec2(texCoords.x * textureSize.x, texCoords.y * textureSize.y));

	//FragColor = byte4(uv_data.x * 255, uv_data.y * 255, 0);
}
