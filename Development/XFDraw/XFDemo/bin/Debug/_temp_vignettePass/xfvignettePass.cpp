//Autogenerated by XFDraw shader parser
#include "xfcore.h"

#include "xfconfig.cpp"




inline void shaderMethod(float* outMultiplier, vec2 viewportMod, vec3 gl_FragCoord){
	float X = (gl_FragCoord.x * viewportMod.x) - 1.0f;
	float Y = (gl_FragCoord.y * viewportMod.y) - 1.0f;
	X = 1.0f - 0.5f * X * X;
	Y = X * (1.0f - 0.5f * Y * Y);
	(*outMultiplier) = Y;
	
}
extern "C" __declspec(dllexport) void ShaderCallFunction(long Width, long Height, unsigned char** ptrPtrs, void* UniformPointer){
	vec2 uniform_0;
	fcpy((char*)(&uniform_0), (char*)UniformPointer + 0, 8);

#pragma omp parallel for
	for (int h = 0; h < Height; ++h){
		int wPos = Width * h;
		float* ptr_0 = (float*)(ptrPtrs[0] + wPos * 4);

		vec3 gl_FragCoord = vec3(0, h, 0);
		for (int w = 0; w < Width; ++w, ++ptr_0, ++gl_FragCoord.x){
			shaderMethod(ptr_0, uniform_0, gl_FragCoord);
		}
	}
}
