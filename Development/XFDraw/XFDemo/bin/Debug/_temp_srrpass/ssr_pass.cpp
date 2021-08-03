out vec3 norm_data;
out vec3 frag_pos;

out byte4 FragColor;


uniform sampler2D colorBuffer;
uniform sampler2D depthBuffer;
uniform samplerCube skybox;


uniform float ray_max_length;
uniform int ray_count;
uniform float ray_count_inverse;
uniform float ray_min_distance;


uniform GLMatrix projection;
uniform float bias;

void main()
{
	FragColor = byte4(frag_pos.x, frag_pos.y, frag_pos.z);
	return;

	if (norm_data.x == 0 && norm_data.y == 0 && norm_data.z == 0)
		return;

	vec3 I = normalize(frag_pos);
	vec3 R = reflect(I, norm_data);

	if (R.z <= 0)
	{
		FragColor = byte4(255, 255, 255);
		return;
	}


	vec3 pos = frag_pos;
	vec3 screen_coord;

	pos = pos + R * ray_min_distance;

	for (int i = 0; i < ray_count; i++)
	{
		pos = pos + R * ray_count_inverse;
		screen_coord = projection * pos;

		if (screen_coord.x >= 1600 || screen_coord.y >= 900 || screen_coord.x < 0 || screen_coord.y < 0)
		{
			FragColor = textureNEAREST(skybox, R);
			return;
		}

		byte4 depth4 = textureNEAREST(depthBuffer, int2(screen_coord.x, screen_coord.y));

		float depth = projection.farZ - *(float*)&depth4;

		if (fabsf(depth - pos.z) <= bias)
		{
			FragColor = textureNEAREST(colorBuffer, int2(screen_coord.x, screen_coord.y));
			return;
		}
	}

	FragColor = textureNEAREST(skybox, R);
}