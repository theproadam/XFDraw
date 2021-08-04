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
	//FragColor = byte4(frag_pos.x, frag_pos.y, frag_pos.z);
	//return;


	if (norm_data.x == 0 && norm_data.y == 0 && norm_data.z == 0)
		return;

	vec3 I = normalize(frag_pos);
	vec3 R = reflect(I, norm_data);

	if (R.z <= 0)
	{
		FragColor = byte4(0, 0, 0);
		return;
	}

	int rayMax = (int)ray_max_length;

	if (rayMax <= 0) rayMax = 1;

	vec3 pos = frag_pos + R * ray_min_distance;
	vec3 pos2 = pos + R;

	vec3 screen_coord = projection * pos;
	vec3 screen_coord2 = projection * pos2;

	pos.z = 1.0f / pos.z;
	pos2.z = 1.0f / pos2.z;

	float deltaX = (screen_coord2.x - screen_coord.x);
	float deltaY = (screen_coord2.y - screen_coord.y);

	if (deltaX * deltaX > deltaY * deltaY)
	{
		float slopeZ = (pos2.z - pos.z) / deltaX;
		float bZ = -slopeZ * screen_coord.x + pos.z;

		float slope = deltaY / deltaX;
		float b = -slope * screen_coord.x + screen_coord.y;

		if (screen_coord2.x > screen_coord.x)
		{
			for (int i = screen_coord.x; i < screen_coord.x + ray_count; i += rayMax)
			{
				int2 sample_coord = int2(i, i * slope + b);
				if (sample_coord.X >= 1600 || sample_coord.Y >= 900 || sample_coord.X < 0 || sample_coord.Y < 0)
				{
					FragColor = byte4(255, 0, 255);
					return;
				}

				byte4 depth4 = textureNEAREST(depthBuffer, sample_coord);
				float depth = projection.farZ - *(float*)&depth4;

				float z_interpol = 1.0f / (i * slopeZ + bZ);

				

				if (abs(depth - z_interpol) <= bias)
				{
					FragColor = textureNEAREST(colorBuffer, sample_coord);
					return;
				}
			}
		}
		else
		{
			for (int i = screen_coord2.x; i < screen_coord2.x + ray_count; i += rayMax)
			{
				int2 sample_coord = int2(i, i * slope + b);
				if (sample_coord.X >= 1600 || sample_coord.Y >= 900 || sample_coord.X < 0 || sample_coord.Y < 0)
				{
					FragColor = byte4(255, 0, 255);
					return;
				}

				byte4 depth4 = textureNEAREST(depthBuffer, sample_coord);
				float depth = projection.farZ - *(float*)&depth4;

				float z_interpol = 1.0f / (i * slopeZ + bZ);

				if (abs(depth - z_interpol) <= bias)
				{
					FragColor = textureNEAREST(colorBuffer, sample_coord);
					return;
				}
			}
		}
	}
	else
	{
		float slopeZ = (pos2.z - pos.z) / deltaY;
		float bZ = -slopeZ * screen_coord.y + pos.z;

		float slope = deltaX / deltaY;
		float b = -slope * screen_coord.y + screen_coord.x;

		if (screen_coord2.y > screen_coord.y)
		{
			for (int i = screen_coord.y; i < screen_coord.y + ray_count; i += rayMax)
			{
				int2 sample_coord = int2(i * slope + b, i);
				if (sample_coord.X >= 1600 || sample_coord.Y >= 900 || sample_coord.X < 0 || sample_coord.Y < 0)
				{
					FragColor = byte4(255, 0, 255);
					return;
				}

				byte4 depth4 = textureNEAREST(depthBuffer, sample_coord);
				float depth = projection.farZ - *(float*)&depth4;

				float z_interpol = 1.0f / (i * slopeZ + bZ);

				if (abs(depth - z_interpol) <= bias)
				{
					FragColor = textureNEAREST(colorBuffer, sample_coord);
					return;
				}
			}
		}
		else
		{
			for (int i = screen_coord2.y; i < screen_coord2.y + ray_count; i += rayMax)
			{
				int2 sample_coord = int2(i * slope + b, i);
				if (sample_coord.X >= 1600 || sample_coord.Y >= 900 || sample_coord.X < 0 || sample_coord.Y < 0)
				{
					FragColor = byte4(255, 0, 255);
					return;
				}

				byte4 depth4 = textureNEAREST(depthBuffer, sample_coord);
				float depth = projection.farZ - *(float*)&depth4;

				float z_interpol = 1.0f / (i * slopeZ + bZ);

				if (abs(depth - z_interpol) <= bias)
				{
					FragColor = textureNEAREST(colorBuffer, sample_coord);
					return;
				}
			}
		}
	}

	FragColor = byte4(255, 255, 255);
}