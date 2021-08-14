out float ssao_buffer;

in vec3 frag_pos;
in vec3 normal;

uniform vec3 cameraPos;
uniform mat3 cameraRot;
uniform GLMatrix cameraProj;

uniform float kernel_radius;
uniform int kernel_size;
uniform sampler1D kernel;

uniform sampler2D depth;
uniform float bias;
uniform float ssao_power;
uniform int FrameCount;

void main()
{
	int offset = (int)gl_FragCoord.y % 2 == 0 ? 1 : 0;
	int offsetX = FrameCount;

	if ((int)(gl_FragCoord.x + offset + FrameCount) % 2 == 0)
		return;

	//ssao_buffer = 0.5f;
//	return;

	vec3 camSpace = cameraRot * (frag_pos - cameraPos);
	float occlusion = 0.0f;
	
	for (int i = 0; i < kernel_size; i++)
	{
		vec3 pos = vec3(texture(kernel, i * 3 + 0), texture(kernel, i * 3 + 1), texture(kernel, i * 3 + 2));

		vec3 samplePos = cameraProj * (camSpace + pos * kernel_radius);

		float sampleDepth = cameraProj.farZ - texture<float>(depth, vec2(samplePos));

		occlusion += (sampleDepth >= samplePos.z + bias ? 1.0f : 0.0f);
	}

	occlusion /= kernel_size;
	occlusion *= ssao_power;

	if (occlusion > 1.0f){
		occlusion = 1.0f;
	}

	if (occlusion < 0){
		occlusion = 0;
	}
	ssao_buffer = 1.0f - occlusion;
}