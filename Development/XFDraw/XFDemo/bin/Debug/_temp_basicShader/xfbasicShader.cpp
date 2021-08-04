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






inline void VSExec(vec3* pos, vec2* uv, vec3* gl_Position, vec2* uv_data, vec3* frag_pos, vec3 cameraPos, mat3 cameraRot){
	(*gl_Position) = cameraRot * ((*pos) * 50.0f - cameraPos);
	(*uv_data) = (*uv);
	(*frag_pos) = (*pos) * 50.0f;
	
}

inline void FSExec(byte4* FragColor, vec2* uv_data, vec3* frag_pos, sampler2D myTexture, vec2 textureSize, vec3 camera_Pos, float heightScale, sampler2D depthMap){
	vec3 viewDir = normalize(camera_Pos - (*frag_pos));
	byte4 reslt = texture(depthMap, vec2((*uv_data).x * textureSize.x, (*uv_data).y * textureSize.y));
	float height = 1.0f - (float)reslt.R * 0.00392156862745f;
	vec2 texCoords = (*uv_data) - vec2(viewDir) * (height * heightScale);
	if (texCoords.x > 1.0 || texCoords.y > 1.0 || texCoords.x < 0.0 || texCoords.y < 0.0){
	return;
	}
	(*FragColor) = texture(myTexture, vec2(texCoords.x * textureSize.x, texCoords.y * textureSize.y));
	
}

void DrawWireFrame(float* VERTEX_DATA, int BUFFER_SIZE, int VW, int VH)
{
	
}

void MethodExec(int index, float* p, float* dptr, char* uData1, char* uData2, unsigned char** ptrPtrs, GLData projData, int facecull, int isWire, MSAAConfig* msaa){
	const int stride = 8;
	const int readStride = 5;
	const int faceStride = 15;
	
	float* VERTEX_DATA = (float*)alloca(stride * 3 * 4);
	int BUFFER_SIZE = 3;
	for (int b = 0; b < 3; ++b){
		float* input = p + (index * faceStride + b * readStride);
		float* output = VERTEX_DATA + b * stride;
		VSExec((vec3*)(input + 0), (vec2*)(input + 3), (vec3*)(output + 0), (vec2*)(output + 3), (vec3*)(output + 5), *(vec3*)(uData1 + 0), *(mat3*)(uData1 + 12));
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
	float ox = projData.ox, oy = projData.oy;
	
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

	if (facecull == 1 || facecull == 2)
	{
		float A = BACKFACECULLS(VERTEX_DATA, stride);
		if (facecull == 2 && A > 0) return RETURN_VALUE;
		else if (facecull == 1 && A < 0) return RETURN_VALUE;
	}

	if (isWire)
	{
		
		return RETURN_VALUE;
	}

	int yMin = (int)yMinValue, yMax = (int)yMaxValue;
	float slopeZ, bZ, s;
	float sA, sB;

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

	for (int i = yMin; i <= yMax; ++i)
	{
		if (ScanLinePLUS(i, VERTEX_DATA, BUFFER_SIZE, Intersects, stride))
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
	
			Z_fptr = dptr + i * renderWidth;
			zBegin = slopeZ * (float)FromX + bZ;

			for (int o = FromX; o <= ToX; ++o ){
				float depth = (1.0f / zBegin);
				s = projData.farZ - depth;
				zBegin += slopeZ;

				if (Z_fptr[o] > s) continue;
				Z_fptr[o] = s;

				if (usingZ) for (int z = 0; z < stride - 3; z++) attribs[z] = (y_Mxb[z] * depth + y_mxB[z]);
				else for (int z = 0; z < stride - 3; z++) attribs[z] = (y_Mxb[z] * (float)o + y_mxB[z]);

				FSExec(ptr_0 + o, (vec2*)(attribs + 0), (vec3*)(attribs + 2), *(sampler2D*)(uData2 + 0), *(vec2*)(uData2 + 24), *(vec3*)(uData2 + 32), *(float*)(uData2 + 44), *(sampler2D*)(uData2 + 48));
			}
		}
	}
}

extern "C" __declspec(dllexport) void ShaderCallFunction(long start, long stop, float* tris, float* dptr, char* uDataVS, char* uDataFS, unsigned char** ptrPtrs, GLData pData, long FACE, long mode, MSAAConfig* msaa)
{
	parallel_for(start, stop, [&](int index){
		MethodExec(index,tris, dptr, uDataVS, uDataFS, ptrPtrs, pData, FACE, mode, msaa);
	});
}