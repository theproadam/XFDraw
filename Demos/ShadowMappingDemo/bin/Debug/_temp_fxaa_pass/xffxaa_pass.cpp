//Autogenerated by XFDraw shader parser
#include "xfcore.h"

#include "xfconfig.cpp"

vec2 min(vec2 a, vec2 b);
vec2 max(vec2 a, vec2 b);
inline float max(float a, float b);
inline float min(float a, float b);
inline float absf(float val);


vec2 min(vec2 a, vec2 b){
	vec2 result = vec2();
	result.x = a.x < b.x ? a.x : b.x;
	result.y = a.y < b.y ? a.y : b.y;
	return result;
	
}
vec2 max(vec2 a, vec2 b){
	vec2 result = vec2();
	result.x = a.x > b.x ? a.x : b.x;
	result.y = a.y > b.y ? a.y : b.y;
	return result;
	
}
inline float max(float a, float b){
	return a > b ? a : b;
	
}
inline float min(float a, float b){
	return a < b ? a : b;
	
}
inline float absf(float val){
	return val > 0 ? val : -val;
	
}

inline void shaderMethod(byte4* currentPixel, byte4* FragColor, sampler2D src, vec3 gl_FragCoord){
	const float FXAA_SPAN_MAX = 8.0f;
	const float FXAA_REDUCE_MUL = 1.0f / 8.0f;
	const float FXAA_REDUCE_MIN = 1.0f / 128.0f;
	vec2 screenXY = vec2(gl_FragCoord);
	vec3 rgbNW = texture(src, screenXY + vec2(-1.0f, -1.0f)).xyz();
	vec3 rgbNE = texture(src, screenXY + vec2(1.0f, -1.0f)).xyz();
	vec3 rgbSW = texture(src, screenXY + vec2(-1.0f, 1.0f)).xyz();
	vec3 rgbSE = texture(src, screenXY + vec2(1.0f, 1.0f)).xyz();
	vec3 rgbM = (*currentPixel).xyz();
	vec3 luma = vec3(0.299f, 0.587f, 0.114f);
	float lumaNW = dot(rgbNW, luma);
	float lumaNE = dot(rgbNE, luma);
	float lumaSW = dot(rgbSW, luma);
	float lumaSE = dot(rgbSE, luma);
	float lumaM = dot(rgbM, luma);
	float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
	float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));
	float range = lumaMax - lumaMin;
	if (!(range <= 0.5f && range >= 0.06f)){
	(*FragColor) = (*currentPixel);
	return;
	}
	vec2 dir;
	dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
	dir.y = ((lumaNW + lumaSW) - (lumaNE + lumaSE));
	float dirReduce = max((lumaNW + lumaNE + lumaSW + lumaSE) * (0.25f * FXAA_REDUCE_MUL), FXAA_REDUCE_MIN);
	float rcpDirMin = 1.0f / (min(absf(dir.x), absf(dir.y)) + dirReduce);
	dir = min(vec2(FXAA_SPAN_MAX, FXAA_SPAN_MAX), max(vec2(-FXAA_SPAN_MAX, -FXAA_SPAN_MAX), dir * rcpDirMin));
	vec3 rgbA = (texture(src, screenXY + dir * (1.0f / 3.0f - 0.5f)).xyz() + texture(src, screenXY + dir * (2.0f / 3.0f - 0.5f)).xyz()) * (1.0f / 2.0f);
	vec3 rgbB = rgbA * (1.0f / 2.0f) + (texture(src, screenXY + dir * (0.0f / 3.0f - 0.5f)).xyz() + texture(src, screenXY + dir * (3.0f / 3.0f - 0.5f)).xyz()) * (1.0f / 4.0f);
	float lumaB = dot(rgbB, luma);
	if ((lumaB < lumaMin) || (lumaB > lumaMax)){
	(*FragColor) = byte4(rgbA.x * 255.0f, rgbA.y * 255.0f, rgbA.z * 255.0f);
	}
	else{
	(*FragColor) = byte4(rgbB.x * 255.0f, rgbB.y * 255.0f, rgbB.z * 255.0f);
	}
	
}
extern "C" __declspec(dllexport) void ShaderCallFunction(long Width, long Height, unsigned char** ptrPtrs, void* UniformPointer){
	sampler2D uniform_0;
	fcpy((char*)(&uniform_0), (char*)UniformPointer + 0, 28);

#pragma omp parallel for
	for (int h = 0; h < Height; ++h){
		int wPos = Width * h;
		byte4* ptr_0 = (byte4*)(ptrPtrs[0] + wPos * 4);

		byte4* ptr_1 = (byte4*)(ptrPtrs[1] + wPos * 4);

		vec3 gl_FragCoord = vec3(0, h, 0);
		for (int w = 0; w < Width; ++w, ++ptr_0, ++ptr_1, ++gl_FragCoord.x){
			shaderMethod(ptr_0, ptr_1, uniform_0, gl_FragCoord);
		}
	}
}
