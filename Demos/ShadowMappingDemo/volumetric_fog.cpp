out byte4 FragColor;
in vec3 world_pos;

uniform int NB_STEPS;
uniform float MAX_LENGTH;
uniform vec3 camera_pos;

uniform mat3 shadow_rot;
uniform vec3 shadow_pos;
uniform vec3 shadow_dir;
uniform GLMatrix shadow_proj;
uniform sampler2D shadow_map;
uniform float ray_bias;
uniform sampler2D noiseMap;
uniform float NB_STEPS_INV;
uniform float noiseX;
uniform float noiseY;

float ComputeScattering(float lightDotView)
{
	const float PI = 3.14159265359f;
	const float G_SCATTERING = 0.1f;

	float result = 1.0f - G_SCATTERING * G_SCATTERING;
	result /= (4.0f * PI * pow(1.0f + G_SCATTERING * G_SCATTERING - (2.0f * G_SCATTERING) * lightDotView, 1.5f));
	return result;
}

void main()
{
	if (world_pos.x == 0 && world_pos.y == 0 && world_pos.z == 0)
		return;

	float step_length = MAX_LENGTH * NB_STEPS_INV;

	vec3 ray_direction = normalize(camera_pos - world_pos);
	vec3 step_size = ray_direction * step_length;

	const float ditherPattern[4][4] = { { 0.0f, 0.5f, 0.125f, 0.625f },
	{ 0.75f, 0.22f, 0.875f, 0.375f },
	{ 0.1875f, 0.6875f, 0.0625f, 0.5625 },
	{ 0.9375f, 0.4375f, 0.8125f, 0.3125 } };

	float ditherValue = ditherPattern[(int)(gl_FragCoord.x) % 4][(int)(gl_FragCoord.y) % 4];

	vec3 ray_pos = world_pos + step_size * ditherValue;

	float f_power = 0.0f;

	for (int i = 0; i < NB_STEPS; i++)
	{
		vec3 shadow_space = shadow_rot * (ray_pos - shadow_pos);
		vec3 shadowXY = shadow_proj * shadow_space;

		float depth = shadow_proj.farZ - textureNEAREST<float>(shadow_map, int2(shadowXY.x, shadowXY.y));

		if (shadowXY.z < depth - ray_bias)
		{
			//f_power += ComputeScattering(dot(ray_direction, shadow_dir)) * 50.0f;
			f_power += 3.0f;
		}

		ray_pos = ray_pos + step_size;
	}

	f_power = f_power * NB_STEPS_INV;

	//f_power *= textureNEAREST<float>(noiseMap, int2(gl_FragCoord.x + noiseX, gl_FragCoord.y + noiseY));
	FragColor = byte4(FragColor.R + f_power * 10.0f, FragColor.G + f_power * 10.0f, FragColor.B + f_power * 10.0f);
}