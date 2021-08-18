//Autogenerated by XFDraw shader parser
#include "xfcore.h"

#include "xfconfig.cpp"

float ComputeScattering(float lightDotView);


float ComputeScattering(float lightDotView){
	const float PI = 3.14159265359f;
	const float G_SCATTERING = 0.1f;
	float result = 1.0f - G_SCATTERING * G_SCATTERING;
	result /= (4.0f * PI * pow(1.0f + G_SCATTERING * G_SCATTERING - (2.0f * G_SCATTERING) * lightDotView, 1.5f));
	return result;
	
}

inline void shaderMethod(vec3* world_pos, byte4* FragColor, int NB_STEPS, float MAX_LENGTH, vec3 camera_pos, mat3 shadow_rot, vec3 shadow_pos, vec3 shadow_dir, GLMatrix shadow_proj, sampler2D shadow_map, float ray_bias, sampler2D noiseMap, float NB_STEPS_INV, float noiseX, float noiseY, int FrameCount, vec3 gl_FragCoord){
	if ((*world_pos).x == 0 && (*world_pos).y == 0 && (*world_pos).z == 0)return;
	int offset = (int)gl_FragCoord.y % 2 == 0 ? 1 : 0;
	int offsetX = FrameCount;
	float step_length = MAX_LENGTH * NB_STEPS_INV;
	vec3 ray_direction = normalize(camera_pos - (*world_pos));
	vec3 step_size = ray_direction * step_length;
	const float ditherPattern[4][4] = {
	 {
	 0.0f, 0.5f, 0.125f, 0.625f }
	,{
	 0.75f, 0.22f, 0.875f, 0.375f }
	,{
	 0.1875f, 0.6875f, 0.0625f, 0.5625 }
	,{
	 0.9375f, 0.4375f, 0.8125f, 0.3125 }
	 }
	;
	float ditherValue = ditherPattern[(int)(gl_FragCoord.x) % 4][(int)(gl_FragCoord.y) % 4];
	vec3 ray_pos = (*world_pos) + step_size * ditherValue;
	float f_power = 0.0f;
	for (int i = 0;
	 i < NB_STEPS;
	 i++){
	vec3 shadow_space = shadow_rot * (ray_pos - shadow_pos);
	vec3 shadowXY = shadow_proj * shadow_space;
	float depth = shadow_proj.farZ - textureNEAREST<float>(shadow_map, int2(shadowXY.x, shadowXY.y));
	if (shadowXY.z < depth - ray_bias){
	f_power += 3.0f;
	}
	ray_pos = ray_pos + step_size;
	}
	f_power = f_power * NB_STEPS_INV;
	(*FragColor) = byte4((*FragColor).R + f_power * 10.0f, (*FragColor).G + f_power * 10.0f, (*FragColor).B + f_power * 10.0f);
	
}
extern "C" __declspec(dllexport) void ShaderCallFunction(long Width, long Height, unsigned char** ptrPtrs, void* UniformPointer){
	int uniform_0;
	fcpy((char*)(&uniform_0), (char*)UniformPointer + 0, 4);
	float uniform_1;
	fcpy((char*)(&uniform_1), (char*)UniformPointer + 4, 4);
	vec3 uniform_2;
	fcpy((char*)(&uniform_2), (char*)UniformPointer + 8, 12);
	mat3 uniform_3;
	fcpy((char*)(&uniform_3), (char*)UniformPointer + 20, 36);
	vec3 uniform_4;
	fcpy((char*)(&uniform_4), (char*)UniformPointer + 56, 12);
	vec3 uniform_5;
	fcpy((char*)(&uniform_5), (char*)UniformPointer + 68, 12);
	GLMatrix uniform_6;
	fcpy((char*)(&uniform_6), (char*)UniformPointer + 80, 56);
	sampler2D uniform_7;
	fcpy((char*)(&uniform_7), (char*)UniformPointer + 136, 28);
	float uniform_8;
	fcpy((char*)(&uniform_8), (char*)UniformPointer + 164, 4);
	sampler2D uniform_9;
	fcpy((char*)(&uniform_9), (char*)UniformPointer + 168, 28);
	float uniform_10;
	fcpy((char*)(&uniform_10), (char*)UniformPointer + 196, 4);
	float uniform_11;
	fcpy((char*)(&uniform_11), (char*)UniformPointer + 200, 4);
	float uniform_12;
	fcpy((char*)(&uniform_12), (char*)UniformPointer + 204, 4);
	int uniform_13;
	fcpy((char*)(&uniform_13), (char*)UniformPointer + 208, 4);

#pragma omp parallel for
	for (int h = 0; h < Height; ++h){
		int wPos = Width * h;
		vec3* ptr_0 = (vec3*)(ptrPtrs[0] + wPos * 12);

		byte4* ptr_1 = (byte4*)(ptrPtrs[1] + wPos * 4);

		vec3 gl_FragCoord = vec3(0, h, 0);
		for (int w = 0; w < Width; ++w, ++ptr_0, ++ptr_1, ++gl_FragCoord.x){
			shaderMethod(ptr_0, ptr_1, uniform_0, uniform_1, uniform_2, uniform_3, uniform_4, uniform_5, uniform_6, uniform_7, uniform_8, uniform_9, uniform_10, uniform_11, uniform_12, uniform_13, gl_FragCoord);
		}
	}
}