//Autogenerated by XFParser

#include <malloc.h>
#include "xfcore.cpp"
#include "xfcore.h"
#include <math.h>
#include <ppl.h>
using namespace Concurrency;
#include "xfconfig.cpp"

#define RETURN_VALUE
#define RtlZeroMemory frtlzeromem






inline void VSExec(vec3* vertex_data, vec3* norm_data, vec3* gl_Position, vec3* normals, vec3 cameraPos, mat3 cameraRot, vec3 objectPos, mat3 objectRot, float zoomMod){
	vec3 pos = cameraRot * (((objectRot * (*vertex_data)) * 0.01f * zoomMod + objectPos) - cameraPos);
	(*normals) = (*norm_data);
	(*gl_Position) = pos;
	
}

inline void FSExec(byte4* FragColor, int* clickIndex, vec3* normals, mat3 camera_rotation, int colorMode, int clickValue, mat3 object_r){
	float opacity = -(((camera_rotation * object_r) * (*normals)).z) * 127.5f + 127.5f;
	if (clickValue == colorMode) opacity *= 0.5f;
	if (colorMode == 1){
	(*FragColor) = byte4(opacity * 0.8f, 50, 50);
	}
	else if (colorMode == 2){
	(*FragColor) = byte4(50, opacity * 0.8f, 50);
	}
	else if (colorMode == 3){
	(*FragColor) = byte4(50, 50, opacity * 0.8f);
	}
	(*clickIndex) = colorMode;
	
}

void DrawLineDATA(float* FromDATA, float* ToDATA, float* dptr, float* attrib, char* uData2, unsigned char** ptrPtrs, float zoffset, int Stride, bool perspMat, float oValue, int offsetmod, int FaceIndex, int VW, int VH, float farZ)
{
	if (FromDATA[0] == ToDATA[0] && FromDATA[1] == ToDATA[1])
		return;

	//Scratch Space Layout
	float* y_Mxb = attrib;
	float* y_mxB = attrib + (Stride - 3);
	float* attribs = attrib + (Stride - 3) * 2;

	float aa = (FromDATA[0] - ToDATA[0]);
	float ba = (FromDATA[1] - ToDATA[1]);
	float zz;

	if (aa * aa > ba * ba)
	{
		float slope = (FromDATA[1] - ToDATA[1]) / (FromDATA[0] - ToDATA[0]);
		float b = -slope * FromDATA[0] + FromDATA[1];

		float slopeZ = (FromDATA[2] - ToDATA[2]) / (FromDATA[0] - ToDATA[0]);
		float bZ = -slopeZ * FromDATA[0] + FromDATA[2];

		for (int s = 3; s < Stride; s++)
		{
			y_Mxb[s - 3] = (FromDATA[s] - ToDATA[s]) / (1.0f / FromDATA[2] - 1.0f / ToDATA[2]);
			y_mxB[s - 3] = -y_Mxb[s - 3] / FromDATA[2] + FromDATA[s];
		}

		if (FromDATA[0] > ToDATA[0])
		{
			float* temp = ToDATA;
			ToDATA = FromDATA;
			FromDATA = temp;
		}

		for (int i = (int)FromDATA[0]; i <= ToDATA[0]; i++)
		{
			int tY = (int)(i * slope + b) + offsetmod;
			float depth = perspMat ? (1.0f / (slopeZ * (float)i + bZ) - oValue) : (slopeZ * (float)i + bZ);

			float s = farZ - depth;
			if (i < 0 || tY < 0 || tY >= VH || i >= VW) continue;

            int mem_addr = VW * tY + i;

			if (dptr[mem_addr] > s - zoffset) continue;
			dptr[mem_addr] = s;

			for (int z = 0; z < Stride - 3; z++)
				attribs[z] = y_Mxb[z] * depth + y_mxB[z];
            
            byte4* ptr_0 = (byte4*)ptrPtrs[0] + mem_addr;
int* ptr_1 = (int*)ptrPtrs[1] + mem_addr;

			FSExec(ptr_0, ptr_1, (vec3*)(attribs + 0), *(mat3*)(uData2 + 0), *(int*)(uData2 + 36), *(int*)(uData2 + 40), *(mat3*)(uData2 + 44));}
	}
	else
	{
		float slope = (FromDATA[0] - ToDATA[0]) / (FromDATA[1] - ToDATA[1]);
		float b = -slope * FromDATA[1] + FromDATA[0];

		float slopeZ = (FromDATA[2] - ToDATA[2]) / (FromDATA[1] - ToDATA[1]);
		float bZ = -slopeZ * FromDATA[1] + FromDATA[2];

		for (int s = 3; s < Stride; s++)
		{
			y_Mxb[s - 3] = (FromDATA[s] - ToDATA[s]) / (1.0f / FromDATA[2] - 1.0f / ToDATA[2]);
			y_mxB[s - 3] = -y_Mxb[s - 3] / FromDATA[2] + FromDATA[s];
		}

		if (FromDATA[1] > ToDATA[1])
		{
			float* temp = ToDATA;
			ToDATA = FromDATA;
			FromDATA = temp;
		}

		for (int i = (int)FromDATA[1]; i <= ToDATA[1]; i++)
		{
            int tY = (int)(i * slope + b) + offsetmod;
			float depth = perspMat ? (1.0f / (slopeZ * (float)i + bZ) - oValue) : (slopeZ * (float)i + bZ);

			float s = farZ - depth;
			if (i < 0 || tY < 0 || tY >= VW || i >= VH) continue;

			int mem_addr = VW * i + tY;

			if (dptr[mem_addr] > s - zoffset) continue;
			dptr[mem_addr] = s;

			for (int z = 0; z < Stride - 3; z++)
				attribs[z] = y_Mxb[z] * depth + y_mxB[z];
            
            byte4* ptr_0 = (byte4*)ptrPtrs[0] + mem_addr;
int* ptr_1 = (int*)ptrPtrs[1] + mem_addr;

			FSExec(ptr_0, ptr_1, (vec3*)(attribs + 0), *(mat3*)(uData2 + 0), *(int*)(uData2 + 36), *(int*)(uData2 + 40), *(mat3*)(uData2 + 44));
		}	
}
}

inline void DrawWireFrame(float* VERTEX_DATA, float* dptr, char* uData2, unsigned char** ptrPtrs, int BUFFER_SIZE, int Stride, GLData projData, int findex, bool perspMat, float oValue, int lineThick, float zoffset)
{
	float* attribs = (float*)alloca((Stride - 3) * 3 * sizeof(float));

	int uppr = (int)((lineThick - 1.0f) / 2.0f);
	int lwr = (int)(lineThick / 2.0f);

	for (int i = 0; i < BUFFER_SIZE - 1; i++)
	{
		for (int s = -lwr; s <= uppr; s++)
			DrawLineDATA(VERTEX_DATA + i * Stride, VERTEX_DATA + (i + 1) * Stride, dptr, attribs, uData2, ptrPtrs, zoffset, Stride, perspMat, oValue, s, findex, projData.renderWidth, projData.renderHeight, projData.farZ);
	}

	for (int s = -lwr; s <= uppr; s++)
		DrawLineDATA(VERTEX_DATA + (BUFFER_SIZE - 1) * Stride, VERTEX_DATA, dptr, attribs, uData2, ptrPtrs, zoffset, Stride, perspMat, oValue, s, findex, projData.renderWidth, projData.renderHeight, projData.farZ);
}

void MethodExec(int index, float* p, float* dptr, char* uData1, char* uData2, unsigned char** ptrPtrs, GLData projData, GLExtra wireData, MSAAConfig* msaa){
	const int stride = 6;
	const int readStride = 6;
	const int faceStride = 18;
	
	float* VERTEX_DATA = (float*)alloca(stride * 3 * 4);
	int BUFFER_SIZE = 3;
	for (int b = 0; b < 3; ++b){
		float* input = p + (index * faceStride + b * readStride);
		float* output = VERTEX_DATA + b * stride;
		VSExec((vec3*)(input + 0), (vec3*)(input + 3), (vec3*)(output + 0), (vec3*)(output + 3), *(vec3*)(uData1 + 0), *(mat3*)(uData1 + 12), *(vec3*)(uData1 + 48), *(mat3*)(uData1 + 60), *(float*)(uData1 + 96));
	}
	
	bool* AP = (bool*)alloca(BUFFER_SIZE + 12);
	frtlzeromem(AP, BUFFER_SIZE);
	
	#pragma region NearPlaneCFG

	int v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * stride + 2] < projData.nearZ)
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return RETURN_VALUE;

#pragma endregion

#pragma region NearPlane
	if (v != 0)
	{
		float* strFLT = (float*)alloca((BUFFER_SIZE * stride + stride) * 4);

		int API = 0;

		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.nearZ, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, projData.nearZ, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.nearZ, stride);

					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else if (i > 0 && AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, projData.nearZ, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
			}
		}

		BUFFER_SIZE = API / stride;
		VERTEX_DATA = strFLT;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}

#pragma endregion

#pragma region FarPlaneCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * stride + 2] > projData.farZ)
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return RETURN_VALUE;

#pragma endregion

#pragma region FarPlane
	if (v != 0)
	{
		float* strFLT = (float*)alloca((BUFFER_SIZE * stride + stride) * 4);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.farZ, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, projData.farZ, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.farZ, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else if (i > 0 && AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, projData.farZ, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / stride;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}
#pragma endregion

#pragma region RightFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * stride + 2] * projData.tanVert + projData.ow < VERTEX_DATA[i * stride])
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return RETURN_VALUE;
#pragma endregion

#pragma region RightFOV
	if (v != 0)
	{
		float* strFLT = (float*)alloca((BUFFER_SIZE * stride + stride) * 4);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.tanVert, projData.ow, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, projData.tanVert, projData.ow, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.tanVert, projData.ow, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, projData.tanVert, projData.ow, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / stride;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}
#pragma endregion

#pragma region LeftFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * stride + 2] * -projData.tanVert - projData.ow > VERTEX_DATA[i * stride])
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return RETURN_VALUE;
#pragma endregion

#pragma region LeftFOV
	if (v != 0)
	{
		float* strFLT = (float*)alloca((BUFFER_SIZE * stride + stride) * 4);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -projData.tanVert, -projData.ow, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -projData.tanVert, -projData.ow, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -projData.tanVert, -projData.ow, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -projData.tanVert, -projData.ow, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / stride;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}
#pragma endregion

#pragma region TopFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * stride + 2] * projData.tanHorz + projData.oh < VERTEX_DATA[i * stride + 1])
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return RETURN_VALUE;

#pragma endregion

#pragma region TopFOV

	if (v != 0)
	{
		float* strFLT = (float*)alloca((BUFFER_SIZE * stride + stride) * 4);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.tanHorz, projData.oh, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, projData.tanHorz, projData.oh, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.tanHorz, projData.oh, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, projData.tanHorz, projData.oh, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / stride;
		RtlZeroMemory(AP, BUFFER_SIZE);


	}

#pragma endregion

#pragma region BottomFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * stride + 2] * -projData.tanHorz - projData.oh > VERTEX_DATA[i * stride + 1])
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return RETURN_VALUE;

#pragma endregion

#pragma region BottomFOV
	if (v != 0)
	{
		float* strFLT = (float*)alloca((BUFFER_SIZE * stride + stride) * 4);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -projData.tanHorz, -projData.oh, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -projData.tanHorz, -projData.oh, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -projData.tanHorz, -projData.oh, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];


					API += stride;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -projData.tanHorz, -projData.oh, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];


					API += stride;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / stride;
	}
#pragma endregion
	int renderWidth = projData.renderWidth, renderHeight = projData.renderHeight;

	float yMaxValue = 0;
	float yMinValue = renderHeight - 1;

	//temp variables ->
	float fwi = 1.0f / projData.fw;
	float fhi = 1.0f / projData.fh;
	float ox = 1.0f / projData.ox, oy = 1.0f / projData.oy;
	
	//XYZ-> XY Transforms
	
    float mMinOne = (1.0f - projData.matrixlerpv);

	if (projData.matrixlerpv == 0)
		for (int im = 0; im < BUFFER_SIZE; im++)
		{
			VERTEX_DATA[im * stride + 0] = roundf(projData.rw + (VERTEX_DATA[im * stride + 0] / VERTEX_DATA[im * stride + 2]) * projData.fw);
			VERTEX_DATA[im * stride + 1] = roundf(projData.rh + (VERTEX_DATA[im * stride + 1] / VERTEX_DATA[im * stride + 2]) * projData.fh);
			VERTEX_DATA[im * stride + 2] = 1.0f / (VERTEX_DATA[im * stride + 2]);

			if (VERTEX_DATA[im * stride + 1] > yMaxValue) yMaxValue = VERTEX_DATA[im * stride + 1];
			if (VERTEX_DATA[im * stride + 1] < yMinValue) yMinValue = VERTEX_DATA[im * stride + 1];
		}
	else if (projData.matrixlerpv == 1)
		for (int im = 0; im < BUFFER_SIZE; im++)
		{
			VERTEX_DATA[im * stride + 0] = roundf(projData.rw + VERTEX_DATA[im * stride + 0] * projData.iox);
			VERTEX_DATA[im * stride + 1] = roundf(projData.rh + VERTEX_DATA[im * stride + 1] * projData.ioy);

			if (VERTEX_DATA[im * stride + 1] > yMaxValue) yMaxValue = VERTEX_DATA[im * stride + 1];
			if (VERTEX_DATA[im * stride + 1] < yMinValue) yMinValue = VERTEX_DATA[im * stride + 1];
		}
	else
		for (int im = 0; im < BUFFER_SIZE; im++)
		{
			VERTEX_DATA[im * stride + 0] = roundf(projData.rw + VERTEX_DATA[im * stride + 0] / ((VERTEX_DATA[im * stride + 2] * fwi - ox) * mMinOne + ox));
			VERTEX_DATA[im * stride + 1] = roundf(projData.rh + VERTEX_DATA[im * stride + 1] / ((VERTEX_DATA[im * stride + 2] * fhi - oy) * mMinOne + oy));
			VERTEX_DATA[im * stride + 2] = 1.0f / (VERTEX_DATA[im * stride + 2] + projData.oValue);


			if (VERTEX_DATA[im * stride + 1] > yMaxValue) yMaxValue = VERTEX_DATA[im * stride + 1];
			if (VERTEX_DATA[im * stride + 1] < yMinValue) yMinValue = VERTEX_DATA[im * stride + 1];
		}

	if (wireData.FACE_CULL == 1 || wireData.FACE_CULL == 2)
	{
		float A = BACKFACECULLS(VERTEX_DATA, stride);
		if (wireData.FACE_CULL == 2 && A > 0) return RETURN_VALUE;
		else if (wireData.FACE_CULL == 1 && A < 0) return RETURN_VALUE;
	}

	if (wireData.WIRE_MODE == 1)
	{
        DrawWireFrame(VERTEX_DATA, dptr, uData2, ptrPtrs, BUFFER_SIZE, stride, projData, index, projData.matrixlerpv != 1, projData.oValue, 1, wireData.offset_wire);
		return RETURN_VALUE;
	}

	int yMin = (int)yMinValue, yMax = (int)yMaxValue;
	float slopeZ, bZ, s;
	float sA, sB;

    if (yMin < 0) yMin = 0;
	if (yMax >= renderHeight) yMax = renderHeight - 1;

	float* Intersects = (float*)alloca((4 + (stride - 3) * 5) * 4);
	float* attribs = Intersects + 4 + (stride - 3) * 2;
	float* y_Mxb = attribs + (stride - 3);
	float* y_mxB = y_Mxb + (stride - 3);

	float* FROM;
	float* TO;

	int FromX, ToX;

	int* RGB_iptr;
	float* Z_fptr;

	float zBegin;
    bool perspMat = projData.matrixlerpv != 1;
	float oValue = projData.oValue;
	float zOffset = wireData.depth_offset;

	for (int i = yMin; i <= yMax; ++i)
	{
		if (ScanLinePLUS(i, VERTEX_DATA, BUFFER_SIZE, Intersects, stride, perspMat))
		{
			if (Intersects[0] > Intersects[stride - 1])
			{
				TO = Intersects;
				FROM = Intersects + (stride - 1);
			}
			else
			{
				FROM = Intersects;
				TO = Intersects + (stride - 1);
			}

			FROM[0] = roundf(FROM[0]);
			TO[0] = roundf(TO[0]);

			//Prevent touching faces from fighting over a scanline pixel

			if (msaa == 0){
			    FromX = (int)FROM[0] == 0 ? 0 : (int)FROM[0] + 1;
			} else {
			    FromX = (int)FROM[0];
			}

			ToX = (int)TO[0];

			//integer truncating doesnt matter here as the float values are already rounded

			slopeZ = (FROM[1] - TO[1]) / (FROM[0] - TO[0]);
			bZ = -slopeZ * FROM[0] + FROM[1];

			//we ignore the TO and FROM here so we have proper interpolation within the range
			if (ToX >= renderWidth) ToX = renderWidth - 1;
			if (FromX < 0) FromX = 0;

			float ZDIFF = 1.0f / FROM[1] - 1.0f / TO[1];
			bool usingZ = ZDIFF != 0;
			//if (ZDIFF != 0) usingZ = ZDIFF * ZDIFF >= 0.0001f;

            usingZ = fabsf(1.0f / FROM[1] - 1.0f / TO[1]) >= 0.2f;
            if (!perspMat) usingZ = false;

			if (usingZ)
			for (int b = 0; b < stride - 3; b++)
			{
				sA = (FROM[2 + b] - TO[2 + b]) / ZDIFF;
				sB = -sA / FROM[1] + FROM[2 + b];

				y_Mxb[b] = sA;
				y_mxB[b] = sB;
			}
			else
			for (int b = 0; b < stride - 3; b++)
			{
				sA = (FROM[2 + b] - TO[2 + b]) / (FROM[0] - TO[0]);
				sB = -sA * FROM[0] + FROM[2 + b];

				y_Mxb[b] = sA;
				y_mxB[b] = sB;
			}

			int wPos = renderWidth * i;
			byte4* ptr_0 = (byte4*)(ptrPtrs[0] + wPos * 4);
	int* ptr_1 = (int*)(ptrPtrs[1] + wPos * 4);
	
			Z_fptr = dptr + i * renderWidth;
			zBegin = slopeZ * (float)FromX + bZ;

			for (int o = FromX; o <= ToX; ++o ){
				float depth = perspMat ? (1.0f / zBegin - oValue) : zBegin;
				s = projData.farZ - depth;
				zBegin += slopeZ;

				if (Z_fptr[o] > s) continue;
				Z_fptr[o] = s;

				    if (usingZ) for (int z = 0; z < stride - 3; z++) attribs[z] = (y_Mxb[z] * depth + y_mxB[z]);
				    else for (int z = 0; z < stride - 3; z++) attribs[z] = (y_Mxb[z] * (float)o + y_mxB[z]);

				FSExec(ptr_0 + o, ptr_1 + o, (vec3*)(attribs + 0), *(mat3*)(uData2 + 0), *(int*)(uData2 + 36), *(int*)(uData2 + 40), *(mat3*)(uData2 + 44));
			}
		}
	}

if (wireData.WIRE_MODE == 2)
	{
		for (int i = 0; i < BUFFER_SIZE - 1; ++i)
			DrawLineNoDATA(VERTEX_DATA + i * stride, VERTEX_DATA + (i + 1) * stride, dptr, wireData.wire_ptr, wireData.wireColor, wireData.offset_wire, stride, projData.renderWidth, projData.renderHeight, projData.farZ);

		DrawLineNoDATA(VERTEX_DATA + (BUFFER_SIZE - 1) * stride, VERTEX_DATA, dptr, wireData.wire_ptr, wireData.wireColor, wireData.offset_wire, stride, projData.renderWidth, projData.renderHeight, projData.farZ);
	}
}

extern "C" __declspec(dllexport) void ShaderCallFunction(long start, long stop, float* tris, float* dptr, char* uDataVS, char* uDataFS, unsigned char** ptrPtrs, GLData pData, GLExtra conf, MSAAConfig* msaa)
{
	parallel_for(start, stop, [&](int index){
		MethodExec(index,tris, dptr, uDataVS, uDataFS, ptrPtrs, pData, conf, msaa);
	});
}