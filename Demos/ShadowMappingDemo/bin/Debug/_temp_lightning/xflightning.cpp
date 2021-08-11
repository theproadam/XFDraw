//Autogenerated by XFDraw shader parser
#include "xfcore.h"

#include "xfconfig.cpp"

float max(float a, float b);


float max(float a, float b){
	if (a < b) return b;
	else return a;
	
}

inline void shaderMethod(vec3* pos, vec3* norm, byte4* objectColor, byte4* FragColor, vec3 lightDir){
	if (*((int*)&((*objectColor))) == 0)return;
	const float ambientStrength = 0.1f;
	vec3 lightColor = vec3(0.8f, 0.8f, 0.8f);
	vec3 ambient = lightColor * ambientStrength;
	float diff = max(dot((*norm), lightDir), 0.0);
	vec3 diffuse = lightColor * diff;
	vec3 result = vec3((*objectColor).R, (*objectColor).G, (*objectColor).B) * (ambient + diffuse);
	(*FragColor) = byte4(result.x, result.y, result.z);
	
}
extern "C" __declspec(dllexport) void ShaderCallFunction(long Width, long Height, unsigned char** ptrPtrs, void* UniformPointer){
	vec3 uniform_0;
	fcpy((char*)(&uniform_0), (char*)UniformPointer + 0, 12);

#pragma omp parallel for
	for (int h = 0; h < Height; ++h){
		int wPos = Width * h;
		vec3* ptr_0 = (vec3*)(ptrPtrs[0] + wPos * 12);

		vec3* ptr_1 = (vec3*)(ptrPtrs[1] + wPos * 12);

		byte4* ptr_2 = (byte4*)(ptrPtrs[2] + wPos * 4);

		byte4* ptr_3 = (byte4*)(ptrPtrs[3] + wPos * 4);

		for (int w = 0; w < Width; ++w, ++ptr_0, ++ptr_1, ++ptr_2, ++ptr_3){
			shaderMethod(ptr_0, ptr_1, ptr_2, ptr_3, uniform_0);
		}
	}
}
