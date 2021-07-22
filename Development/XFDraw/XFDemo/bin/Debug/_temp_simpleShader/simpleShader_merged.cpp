//Autogenerated by XFDraw shader parser
#include "simpleShader_header.h"




inline void shaderMethod(int* color, vec2 viewportMod, int someValue, vec3 gl_FragCoord){
	float X = (gl_FragCoord.x * viewportMod.x) - 1.0f;
	float Y = (gl_FragCoord.y * viewportMod.y) - 1.0f;
	X = 1.0f - 0.5f * X * X;
	Y = X * (1.0f - 0.5f * Y * Y);
	unsigned char R = 255 * Y;
	unsigned char G = 255 * Y;
	unsigned char B = 0;
	(*color) = someValue;
	
}
extern "C" __declspec(dllexport) void ShaderCallFunction(long Width, long Height, unsigned char** ptrPtrs, void* UniformPointer){
	vec2 uniform_0;
	fcpy((char*)(&uniform_0), (char*)UniformPointer + 0, 8);
	int uniform_1;
	fcpy((char*)(&uniform_1), (char*)UniformPointer + 8, 4);

#pragma omp parallel for
	for (int h = 0; h < Height; ++h){
		int wPos = Width * h;
		int* ptr_0 = (int*)(ptrPtrs[0] + wPos * 4);

		vec3 gl_FragCoord = vec3(0, h, 0);
		for (int w = 0; w < Width; ++w, ++ptr_0, ++gl_FragCoord.x){
			shaderMethod(ptr_0, uniform_0, uniform_1, gl_FragCoord);
		}
	}
}
