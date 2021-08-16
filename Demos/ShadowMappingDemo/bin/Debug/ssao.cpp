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

uniform sampler2D ssao_noise;

inline vec3 rotateZAxis(vec3 input, vec2 rot)
{
	return vec3(input.x * rot.x - input.y * rot.y, input.x * rot.y + input.y * rot.x, input.z);
}


inline float clamp01(float val)
{
	if (val > 1.0f) return 1.0f;
	else if (val < 0.0f) return 0.0f;
	else return val;
}

inline float smoothstep(float edge0, float edge1, float x)
{
	float t = clamp01((x - edge0) / (edge1 - edge0));
	return t * t * (3.0 - 2.0 * t);
}

void main()
{
	vec3 camSpace = cameraRot * (frag_pos - cameraPos);
	float occlusion = 0.0f;
	


//	color = byte4(randomness.x * 127.5f + 127.5f, randomness.y * 127.5f + 127.5f, 255);

	//return;
	for (int i = 0; i < kernel_size; i++)
	{
		vec3 pos = texture<vec3>(kernel, i);

	//	vec2 randomness = texture<vec2>(ssao_noise, vec2(gl_FragCoord));
	//	pos = rotateZAxis(pos, randomness);

		vec3 samplePos = cameraProj * (camSpace + pos * kernel_radius);		
		
		float sampleDepth = cameraProj.farZ - texture<float>(depth, vec2(samplePos));
		//float rangeCheck = smoothstep(0.0f, 1.0f, kernel_radius / abs(camSpace.z - sampleDepth));
	//	float rangeCheck = abs(frag_pos.z, )
		float rangeCheck = 1.0f;

		if (abs(camSpace.z - sampleDepth) > 10.0f)
			rangeCheck = 0.0f;

		if (sampleDepth <= samplePos.z - bias)
		{
			occlusion += 1.0f * rangeCheck;
		}

		//occlusion += (sampleDepth >= samplePos.z + bias ? 1.0f : 0.0f) * rangeCheck;
	}

	occlusion /= kernel_size;
	occlusion *= ssao_power;

	if (occlusion > 1.0f){
		occlusion = 1.0f;
	}

	if (occlusion < 0.0f){
		occlusion = 0;
	}

	ssao_buffer = 1.0f - occlusion;
}