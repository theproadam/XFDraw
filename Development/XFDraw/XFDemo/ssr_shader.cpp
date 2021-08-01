//version 330 Core
out byte4 FragColor;

in vec3 norm_data;
in vec3 frag_pos;

uniform samplerCube skybox;
uniform int sample_count;
uniform mat3 rotation;

uniform sampler2D colorBuffer;

void main()
{
	
	vec3 I = (frag_pos - camera_Pos);
	vec3 R = reflect(I, norm_data);

	vec2 searchStart = gl_FragCoord;
	vec3 searchLocation = gl_FragPos;
	vec3 searchDir = rotation * norm_data
	
	if (searchDir.z <= 0)
	{
		FragColor = byte4(255, 255, 255);
		return;
	}

	if (searchDir.x > searchdir.y)
	{
		float slope = searchdir.y / searchdir.x;
		
		for (int i = 0; i < sample_count; i++)
		{
			searchStart += vec2(1.0f, slope);

			if (searchStart)
			{
				
			}
		}	
	}

	
	
	FragColor = textureNEAREST(skybox, R);
}
