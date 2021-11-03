#include "stdafx.h"
#include <omp.h>
#include "XFCore.h"
#include <ppl.h>
#include <atomic>


using namespace Concurrency;

#define byte unsigned char
#define RETURN_VALUE

unsigned char Clamp255(float value)
{
	if (value > 255)
		return 255;
	else return (byte)value;
}

void FillSkybox(int index, float* p, int* rd, GLData projData, mat3 rotMatrix, float** sptr, int* bsptr,  float* sdptr){
	const int stride = 5;
	const int readStride = 5;
	const int faceStride = 15;

	float* VERTEX_DATA = (float*)alloca(stride * 3 * 4);
	int BUFFER_SIZE = 3;
	for (int b = 0; b < 3; ++b){
		float* input = p + (index * faceStride + b * readStride);
		float* output = VERTEX_DATA + b * stride;

		vec3 xyz = *(vec3*)(input + 0);
		vec2 uv = *(vec2*)(input + 3);

		*((vec3*)(output + 0)) = rotMatrix * xyz;
		*((vec2*)(output + 3)) = uv;
	}

	bool* AP = (bool*)alloca(BUFFER_SIZE + 12);
	RtlZeroMemory(AP, BUFFER_SIZE);

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

	float yMaxValue = 0;
	float yMinValue = projData.renderHeight - 1;

	for (int im = 0; im < BUFFER_SIZE; im++)
	{
		VERTEX_DATA[im * stride + 0] = roundf(projData.rw + (VERTEX_DATA[im * stride + 0] / VERTEX_DATA[im * stride + 2]) * projData.fw);
		VERTEX_DATA[im * stride + 1] = roundf(projData.rh + (VERTEX_DATA[im * stride + 1] / VERTEX_DATA[im * stride + 2]) * projData.fh);
		VERTEX_DATA[im * stride + 2] = 1.0f / (VERTEX_DATA[im * stride + 2]);

		if (VERTEX_DATA[im * stride + 1] > yMaxValue) yMaxValue = VERTEX_DATA[im * stride + 1];
		if (VERTEX_DATA[im * stride + 1] < yMinValue) yMinValue = VERTEX_DATA[im * stride + 1];
	}

	int yMax = (int)yMaxValue;
	int yMin = (int)yMinValue;
	
	
	int t = (*rd)++;

	for (int i = yMin; i <= yMax; i++)
	{
		int lt = bsptr[i]++;
		sptr[i * 12 + lt] = sdptr + t * 77;
	}

	sdptr[t * 77] = BUFFER_SIZE;
	sdptr[t * 77 + 1] = index;

	for (int i = 0; i < BUFFER_SIZE * stride; i++)
	{
		sdptr[t * 77 + (i + 2)] = VERTEX_DATA[i];
	}

}

void SkyPass(int i, int* iptr, int renderWidth, int* bsptr, int skyboxSize, float** sptr, int** txptr)
{
	const int Stride = 5;

	//float* Intersects = stackalloc float[4 + (Stride - 3) * 5];
	float* Intersects = (float*)alloca((4 + (Stride - 3) * 5) * 4);
	RtlZeroMemory(Intersects, (4 + (Stride - 3) * 5) * 4);
	float* az = Intersects + 4 + (Stride - 3) * 2;
	float* slopeAstack = az + (Stride - 3);
	float* bAstack = slopeAstack + (Stride - 3);

	int FACE_COUNT = bsptr[i];

	float sA;
	float sB;

	float slopeZ;
	float bZ;

	int X;
	int Y;

	float* FROM;
	float* TO;

	int FromX;
	int ToX;

	int Addr;
	int sizemone = skyboxSize - 1;
	int maxaddr = (skyboxSize * skyboxSize) - 1;

	float slopeU;
	float slopeV;

	float bU;
	float bV;

	for (int t = 0; t < FACE_COUNT; t++)
	{
		int BUFFER_SIZE = (int)*(sptr + 12 * i + t)[0];
		int* smpl = txptr[(int)sptr[12 * i + t][1]];

		if (ScanLinePLUS_(i, *(sptr + 12 * i + t) + 2, BUFFER_SIZE, Intersects, Stride))
		{
			if (Intersects[0] > Intersects[Stride - 1])
			{
				TO = Intersects;
				FROM = Intersects + (Stride - 1);
			}
			else
			{
				FROM = Intersects;
				TO = Intersects + (Stride - 1);
			}

			FROM[0] = roundf(FROM[0]);
			TO[0] = roundf(TO[0]);

			slopeZ = (FROM[1] - TO[1]) / (FROM[0] - TO[0]);
			bZ = -slopeZ * FROM[0] + FROM[1];

			FromX = (int)FROM[0] == 0 ? 0 : (int)FROM[0] + 1;
			ToX = (int)TO[0];

			if (ToX >= renderWidth) TO[0] = renderWidth - 1;
			if (FromX < 0) FROM[0] = 0;

			float ZDIFF = 1.0f / FROM[1] - 1.0f / TO[1];
			bool usingZ = ZDIFF != 0;
			if (ZDIFF != 0) usingZ = ZDIFF * ZDIFF >= 0.00001f;


			if (usingZ)
			{
				sA = (FROM[2] - TO[2]) / ZDIFF;
				sB = -sA / FROM[1] + FROM[2];

				slopeU = sA * sizemone;
				bU = sB * sizemone;

				sA = (FROM[3] - TO[3]) / ZDIFF;
				sB = -sA / FROM[1] + FROM[3];

				slopeV = sA * sizemone;
				bV = sB * sizemone;
			}
			else
			{
				sA = (FROM[2] - TO[2]) / (FROM[0] - TO[0]);
				sB = -sA * FROM[0] + FROM[2];

				slopeU = sA * sizemone;
				bU = sB * sizemone;

				sA = (FROM[3] - TO[3]) / (FROM[0] - TO[0]);
				sB = -sA * FROM[0] + FROM[3];

				slopeV = sA * sizemone;
				bV = sB * sizemone;
			}
			//Leftover code for debugging:
			//   byte* addr = bptr + (i * wsD + (FromX * sD) + 0);
			//   int* addr = iptr + (i * renderWidth + FromX + 0);
			//   addr[0] = (byte)(az[0] * 255f);
			//   addr[1] = (byte)(az[1] * 255f);
			//   addr++;
			

			int* addr = iptr + (i * renderWidth);

			float begin = slopeZ * (float)FromX + bZ;
			float begin1 = slopeU * (float)FromX + bU;
			float begin2 = slopeV * (float)FromX + bV;

			if (usingZ)
			for (int o = FromX; o <= ToX; ++o)
			{
				float z_i = 1.0f / begin;

				X = (int)(slopeU * z_i + bU);
				Y = (int)(slopeV * z_i + bV);
				begin += slopeZ;

				Addr = Y * skyboxSize + X;

				if (Addr > maxaddr || Addr < 0)
				{
					continue;
				}

				addr[o] = smpl[Addr];
			}
			else
			for (int o = FromX; o <= ToX; ++o)
			{
				X = (int)(begin1);
				Y = (int)(begin2);
				begin1 += slopeU;
				begin2 += slopeV;

				Addr = Y * skyboxSize + X;

				if (Addr > maxaddr || Addr < 0)
				{
					continue;
				}

				addr[o] = smpl[Addr];
			}

		}
	}
}

inline float absf(float val)
{
	return val > 0 ? val : -val;
}

#define byteToRGB 0.00392156862745f
#define lumaByte 0.0000153787004998f

inline float dotByte(byte4 lhs, byte4 rhs)
{
	return lumaByte * (lhs.R * rhs.R + lhs.G * rhs.G + lhs.B * rhs.B);
}

extern "C"
{
	__declspec(dllexport) void VignettePass(long* TargetBuffer, float* SourceBuffer, long Width, long Height)
	{
		if (ForceUseOpenMP)
		{
#pragma omp parallel for
			for (int h = 0; h < Height; ++h)
			{
				unsigned char* bptr = (unsigned char*)(TargetBuffer + Width * h);
				float* fptr = SourceBuffer + Width * h;

				for (int w = 0; w < Width; ++w, bptr += 4, ++fptr)
				{
					bptr[0] *= *fptr;
					bptr[1] *= *fptr;
					bptr[2] *= *fptr;
				}
			}
		}
		else
		{
			int hght = Height;
			int wdth = Width;
			parallel_for(0, hght, [&](int h)
			{
				unsigned char* bptr = (unsigned char*)(TargetBuffer + Width * h);
				float* fptr = SourceBuffer + Width * h;

				for (int w = 0; w < wdth; ++w, bptr += 4, ++fptr)
				{
					bptr[0] *= *fptr;
					bptr[1] *= *fptr;
					bptr[2] *= *fptr;
				}
			});

		}
	}

	__declspec(dllexport) void VignettePass2(long* TargetBuffer, float* SourceBuffer, long Width, long Height)
	{
#pragma omp parallel for num_threads(8)
		for (int h = 0; h < Height; ++h)
		{
			unsigned char* bptr = (unsigned char*)(TargetBuffer + Width * h);
			int* iptr = (int*)TargetBuffer + h * Width;
			float* fptr = SourceBuffer + Width * h;

			for (int w = 0; w < Width; ++w, bptr += 4, ++fptr)
			{
				//bptr[0] *= *fptr;
				//bptr[1] *= *fptr;
				//bptr[2] *= *fptr;
				iptr[w] = (unsigned char)(bptr[0] * *fptr) + 256 * (unsigned char)(bptr[1] * *fptr) + 65536 * (unsigned char)(bptr[2] * *fptr);

			}
		}
	}

	__declspec(dllexport) void DrawSkybox(float* tris, int* iptr, long skyBoxWidth, GLData projData, mat3 rotMatrix, float** sptr, int* bsptr, int** txptr, float* sdptr)
	{
		int rd = 0;

		for (int i = 0; i < 12; i++)
			FillSkybox(i, tris, &rd, projData, rotMatrix, sptr, bsptr, sdptr);

		//	parallel_for(0, projData.renderHeight, [&](int i){
		//		SkyPass(i, iptr, projData.renderWidth, bsptr, skyBoxWidth, sptr, txptr);
		//	});

		//#pragma omp parallel for
		//	for (int i = 0; i < projData.renderHeight; i++)


		parallel_for(0, projData.renderHeight, [&](int i)
		{
			SkyPass(i, iptr, projData.renderWidth, bsptr, skyBoxWidth, sptr, txptr);
		});

	}

	__declspec(dllexport) void MSAA_Merge(long* TargetBuffer, long** ptrPtrs, long count, long Width, long Height)
	{
		if (count == 2)
		{
#pragma omp parallel for
			for (int h = 0; h < Height; ++h)
			{
				byte* bptr = (byte*)(TargetBuffer + h * Width);
				byte* bptr1 = (byte*)(ptrPtrs[0] + h * Width);
				byte* bptr2 = (byte*)(ptrPtrs[1] + h * Width);

				for (int w = 0; w < Width; ++w, bptr += 4, bptr1 += 4, bptr2 += 4)
				{
					bptr[0] = Clamp255(bptr1[0] * 0.50f + bptr2[0] * 0.50f);
					bptr[1] = Clamp255(bptr1[1] * 0.50f + bptr2[1] * 0.50f);
					bptr[2] = Clamp255(bptr1[2] * 0.50f + bptr2[2] * 0.50f);
				}
			}
		}
		else if (count == 4)
		{
#pragma omp parallel for
			for (int h = 0; h < Height; ++h)
			{
				byte* bptr = (byte*)(TargetBuffer + h * Width);
				byte* bptr1 = (byte*)(ptrPtrs[0] + h * Width);
				byte* bptr2 = (byte*)(ptrPtrs[1] + h * Width);
				byte* bptr3 = (byte*)(ptrPtrs[2] + h * Width);
				byte* bptr4 = (byte*)(ptrPtrs[3] + h * Width);

				for (int w = 0; w < Width; ++w, bptr += 4, bptr1 += 4, bptr2 += 4, bptr3 += 4, bptr4 += 4)
				{
					bptr[0] = Clamp255(bptr1[0] * 0.25f + bptr2[0] * 0.25f + bptr3[0] * 0.25f + bptr4[0] * 0.25f);
					bptr[1] = Clamp255(bptr1[1] * 0.25f + bptr2[1] * 0.25f + bptr3[1] * 0.25f + bptr4[1] * 0.25f);
					bptr[2] = Clamp255(bptr1[2] * 0.25f + bptr2[2] * 0.25f + bptr3[2] * 0.25f + bptr4[2] * 0.25f);
				}
			}
		}
		else if (count == 8)
		{
#pragma omp parallel for
			for (int h = 0; h < Height; ++h)
			{
				byte* bptr = (byte*)(TargetBuffer + h * Width);
				byte* bptr1 = (byte*)(ptrPtrs[0] + h * Width);
				byte* bptr2 = (byte*)(ptrPtrs[1] + h * Width);
				byte* bptr3 = (byte*)(ptrPtrs[2] + h * Width);
				byte* bptr4 = (byte*)(ptrPtrs[3] + h * Width);
				byte* bptr5 = (byte*)(ptrPtrs[4] + h * Width);
				byte* bptr6 = (byte*)(ptrPtrs[5] + h * Width);
				byte* bptr7 = (byte*)(ptrPtrs[6] + h * Width);
				byte* bptr8 = (byte*)(ptrPtrs[7] + h * Width);

				for (int w = 0; w < Width; ++w, bptr += 4, bptr1 += 4, bptr2 += 4, bptr3 += 4, bptr4 += 4)
				{
					bptr[0] = Clamp255(bptr1[0] * 0.25f + bptr2[0] * 0.25f + bptr3[0] * 0.25f + bptr4[0] * 0.25f + bptr5[0] * 0.25 + bptr6[0] * 0.25 + bptr7[0] * 0.25 + bptr8[0] * 0.25);
					bptr[1] = Clamp255(bptr1[1] * 0.25f + bptr2[1] * 0.25f + bptr3[1] * 0.25f + bptr4[1] * 0.25f + bptr5[1] * 0.25 + bptr6[1] * 0.25 + bptr7[1] * 0.25 + bptr8[1] * 0.25);
					bptr[2] = Clamp255(bptr1[2] * 0.25f + bptr2[2] * 0.25f + bptr3[2] * 0.25f + bptr4[2] * 0.25f + bptr5[2] * 0.25 + bptr6[2] * 0.25 + bptr7[2] * 0.25 + bptr8[2] * 0.25);
				}
			}
		}
	}

	__declspec(dllexport) void MSAA_Copy(long* TargetBuffer, long** ptrPtrs, long count, long Width, long Height)
	{
		if (count != 4)
			return;

#pragma omp parallel for
		for (int h = 0; h < Height; ++h)
		{
			int* iptr = (int*)(TargetBuffer + h * Width);
			int* iptr1 = (int*)(ptrPtrs[0] + h * Width);
			int* iptr2 = (int*)(ptrPtrs[1] + h * Width);
			int* iptr3 = (int*)(ptrPtrs[2] + h * Width);
			int* iptr4 = (int*)(ptrPtrs[3] + h * Width);

			for (int w = 0; w < Width; ++w, iptr++, iptr1++, iptr2++, iptr3++, iptr4++)
			{
				*iptr1 = *iptr;
				*iptr2 = *iptr;
				*iptr3 = *iptr;
				*iptr4 = *iptr;
			}
		}
	}

	__declspec(dllexport) void SSR_PASS(int* TargetBuffer, vec3* norm_data, vec3* pos_data, long count, long Width, long Height)
	{

	}

	__declspec(dllexport) void ResizeFast(int* TargetBuffer, int* SourceBuffer, long wDest, long hDest, long wSrc, long hSrc, long mode)
	{
		float factor_x = (float)wSrc / (float)wDest;
		float factor_y = (float)hSrc / (float)hDest;


#pragma omp parallel for
		for (int h = 0; h < hDest; ++h)
		{
			int* dest = (int*)(TargetBuffer + wDest * h);
			int* src = (int*)(TargetBuffer + wSrc * (int)(h * factor_y));

			for (int w = 0; w < wDest; ++w)
			{
				dest[w] = src[(int)(w * factor_x)];
			}
		}

	}

	__declspec(dllexport) void FXAA_PASS(int* TargetBuffer, int* SourceBuffer, long width, long height)
	{
		int wsD = width * 4;

#pragma omp parallel for
		for (int h = 1; h < height - 1; h++)
		{
			unsigned char* bptr = (unsigned char*)SourceBuffer + h * wsD + 4;
			int* tBuf = TargetBuffer + h * width;
			int* sBuf = SourceBuffer + h * width;

			tBuf[0] = sBuf[0];
			tBuf++;
			sBuf++;

			int Ydelta;
			int Xdelta;

			for (int w = 1; w < width - 1; w++, bptr += 4, tBuf++, sBuf++)
			{
				vec3 rgbM = vec3(bptr[2] * 0.00392156862745f, bptr[1] * 0.00392156862745f, bptr[0] * 0.00392156862745f);
				vec3 rgbNE = vec3(bptr[2 + 4] * byteToRGB, bptr[1 + 4] * byteToRGB, bptr[0 + 4] * byteToRGB);
				vec3 rgbNW = vec3(bptr[2 - 4] * byteToRGB, bptr[1 - 4] * byteToRGB, bptr[0 - 4] * byteToRGB);
				vec3 rgbSW = vec3(bptr[2 + wsD] * byteToRGB, bptr[1 + wsD] * byteToRGB, bptr[0 + wsD] * byteToRGB);
				vec3 rgbSE = vec3(bptr[2 - wsD] * byteToRGB, bptr[1 - wsD] * byteToRGB, bptr[0 - wsD] * byteToRGB);

				vec3 luma = vec3(0.299f, 0.587f, 0.114f);
				float lumaNW = dot(rgbNW, luma);
				float lumaNE = dot(rgbNE, luma);
				float lumaSW = dot(rgbSW, luma);
				float lumaSE = dot(rgbSE, luma);
				float lumaM = dot(rgbM, luma);

				float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
				float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));

				float range = lumaMax - lumaMin;

				if (range <= 0.5f && range >= 0.06f)
				{
					float delta1 = absf(lumaNW - lumaNE); //horizontal smear
					float delta2 = absf(lumaSW - lumaSE); //vertical smear

					float norm = 1.0f / (delta1 + delta2);
					delta1 *= norm;
					delta2 *= norm;

					vec3 val = (rgbNW * 0.5f + rgbNE * 0.5f) * delta2 + (rgbSW * 0.5f + rgbSE * 0.5f) * delta1;// +rgbM * 0.6f;
					val.Clamp01();

					((unsigned char*)tBuf)[0] = val.z * 255.0f;
					((unsigned char*)tBuf)[1] = val.y * 255.0f;
					((unsigned char*)tBuf)[2] = val.x * 255.0f;

					continue;
				}
				tBuf[0] = sBuf[0];
			}

			tBuf[0] = sBuf[0];

		}

		//these loops could theoretically be parallelized

		for (int w = 0; w < width - 1; w++)
		{
			TargetBuffer[w] = SourceBuffer[w];
		}

		int offset = (height - 1) * width;


		for (int w = 0; w < width; w++)
		{
			TargetBuffer[offset + w] = SourceBuffer[offset + w];
		}

	}

	__declspec(dllexport) void FXAA_PASS2(byte4* TargetBuffer, byte4* SourceBuffer, long width, long height)
	{
		int wsD = width * 4;

#pragma omp parallel for
		for (int h = 1; h < height - 1; h++)
		{
			unsigned char* bptr = (unsigned char*)SourceBuffer + h * wsD + 4;
			byte4* tBuf = TargetBuffer + h * width;
			byte4* sBuf = SourceBuffer + h * width;

			tBuf[0] = sBuf[0];
			tBuf++;
			sBuf++;

			int Ydelta;
			int Xdelta;

			for (int w = 1; w < width - 1; w++, bptr += 4, tBuf++, sBuf++)
			{
				byte4 rgbM = sBuf[0];
				byte4 rgbNE = sBuf[1];
				byte4 rgbNW = sBuf[-1];
				byte4 rgbSW = sBuf[width];
				byte4 rgbSE = sBuf[-width];

				//vec3 luma = vec3(0.299f, 0.587f, 0.114f);
				byte4 luma = byte4(0.299f * 255.0f, 0.587f * 255.0f, 0.114f * 255.0f);
				float lumaNW = dotByte(rgbNW, luma);
				float lumaNE = dotByte(rgbNE, luma);
				float lumaSW = dotByte(rgbSW, luma);
				float lumaSE = dotByte(rgbSE, luma);
				float lumaM = dotByte(rgbM, luma);

				float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
				float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));

				float range = lumaMax - lumaMin;

				if (range <= 0.5f && range >= 0.06f)
				{
					float delta1 = absf(lumaNW - lumaNE); //horizontal smear
					float delta2 = absf(lumaSW - lumaSE); //vertical smear

					float norm = 1.0f / (delta1 + delta2);
					delta1 *= norm;
					delta2 *= norm;

					//vec3 val = (rgbNW * 0.5f + rgbNE * 0.5f) * delta2 + (rgbSW * 0.5f + rgbSE * 0.5f) * delta1;// +rgbM * 0.6f;

					//unsigned char R = (rgbNW.R * 0.5f + rgbNE.R * 0.5f) * delta2 + (rgbSW.R * 0.5f + rgbSE.R * 0.5f) * delta1;
					//unsigned char G = (rgbNW.G * 0.5f + rgbNE.G * 0.5f) * delta2 + (rgbSW.G * 0.5f + rgbSE.G * 0.5f) * delta1;
					//unsigned char B = (rgbNW.B * 0.5f + rgbNE.B * 0.5f) * delta2 + (rgbSW.B * 0.5f + rgbSE.B * 0.5f) * delta1;

					float R = (rgbNW.R * 0.5f + rgbNE.R * 0.5f) * delta2 + (rgbSW.R * 0.5f + rgbSE.R * 0.5f) * delta1;
					float G = (rgbNW.G * 0.5f + rgbNE.G * 0.5f) * delta2 + (rgbSW.G * 0.5f + rgbSE.G * 0.5f) * delta1;
					float B = (rgbNW.B * 0.5f + rgbNE.B * 0.5f) * delta2 + (rgbSW.B * 0.5f + rgbSE.B * 0.5f) * delta1;

					if (R > 255.0f) R = 255;
					if (G > 255.0f) G = 255;
					if (B > 255.0f) B = 255;


					byte4 reslt = byte4(R, G, B);

					tBuf[0] = reslt;
					//	((unsigned char*)tBuf)[0] = val.z * 255.0f;
					//	((unsigned char*)tBuf)[1] = val.y * 255.0f;
					//	((unsigned char*)tBuf)[2] = val.x * 255.0f;

					continue;
				}
				tBuf[0] = sBuf[0];
			}

			tBuf[0] = sBuf[0];

		}

		//these loops could theoretically be parallelized

		for (int w = 0; w < width - 1; w++)
		{
			TargetBuffer[w] = SourceBuffer[w];
		}

		int offset = (height - 1) * width;


		for (int w = 0; w < width; w++)
		{
			TargetBuffer[offset + w] = SourceBuffer[offset + w];
		}

	}

	__declspec(dllexport) void BOX_BLUR(byte4* TargetBuffer, byte4* SourceBuffer, byte4* TempBuffer, long width, long height)
	{
		int wsD = width * 4;

		//HORIZONTAL PASS
#pragma omp parallel for
		for (int h = 0; h < height; h++)
		{
			byte4* tBuf = TempBuffer + h * width;
			byte4* sBuf = SourceBuffer + h * width;

			tBuf[0] = sBuf[0];
			tBuf++;
			sBuf++;

			for (int w = 1; w < width - 1; w++, tBuf++, sBuf++)
			{
				byte4 rght = sBuf[-1];	
				byte4 cntr = sBuf[0];
				byte4 left = sBuf[1];

				unsigned char R = (left.R + cntr.R + rght.R) * 0.3333333f;
				unsigned char G = (left.G + cntr.G + rght.G) * 0.3333333f;
				unsigned char B = (left.B + cntr.B + rght.B) * 0.3333333f;

				tBuf[0] = byte4(R, G, B);
			}

			tBuf[0] = sBuf[0];
		}

		//VERTICAL PASS
#pragma omp parallel for
		for (int w = 0; w < width; w++)
		{
			byte4* tBuf = TargetBuffer + w;
			byte4* sBuf = TempBuffer + w;

			tBuf[0] = sBuf[0];

			sBuf += width;
			tBuf += width;

			for (int h = 1; h < height - 1; h++, tBuf += width, sBuf += width)
			{
				byte4 down = sBuf[-width];
				byte4 cntr = sBuf[0];
				byte4 uppr = sBuf[width];

				unsigned char R = (down.R + cntr.R + uppr.R) * 0.3333333f;
				unsigned char G = (down.G + cntr.G + uppr.G) * 0.3333333f;
				unsigned char B = (down.B + cntr.B + uppr.B) * 0.3333333f;

				tBuf[0] = byte4(R, G, B);
			}

			tBuf[0] = sBuf[0];
		}


	}

	__declspec(dllexport) void BOX_BLUR_FLOAT(float* TargetBuffer, float* SourceBuffer, float* TempBuffer, long width, long height)
	{
		int wsD = width * 4;

		//HORIZONTAL PASS
#pragma omp parallel for
		for (int h = 0; h < height; h++)
		{
			float* tBuf = TempBuffer + h * width;
			float* sBuf = SourceBuffer + h * width;

			tBuf[0] = sBuf[0];
			tBuf++;
			sBuf++;

			for (int w = 1; w < width - 1; w++, tBuf++, sBuf++)
			{
				float rght = sBuf[-1];
				float cntr = sBuf[0];
				float left = sBuf[1];

				tBuf[0] = (left + cntr + rght) * 0.3333333f;
			}

			tBuf[0] = sBuf[0];
		}

		//VERTICAL PASS
#pragma omp parallel for
		for (int w = 0; w < width; w++)
		{
			float* tBuf = TargetBuffer + w;
			float* sBuf = TempBuffer + w;

			tBuf[0] = sBuf[0];

			sBuf += width;
			tBuf += width;

			for (int h = 1; h < height - 1; h++, tBuf += width, sBuf += width)
			{
				float down = sBuf[-width];
				float cntr = sBuf[0];
				float uppr = sBuf[width];

				tBuf[0] = (down + cntr + uppr) * 0.3333333f;
			}

			tBuf[0] = sBuf[0];
		}


	}

	__declspec(dllexport) void BOX_BLUR5(byte4* TargetBuffer, byte4* SourceBuffer, byte4* TempBuffer, long width, long height)
	{
		int width2 = width * 2;

		//HORIZONTAL PASS
#pragma omp parallel for
		for (int h = 0; h < height; h++)
		{
			byte4* tBuf = TempBuffer + h * width;
			byte4* sBuf = SourceBuffer + h * width;

			tBuf[0] = sBuf[0];
			tBuf++;
			sBuf++;
			tBuf[0] = sBuf[0];
			tBuf++;
			sBuf++;

			for (int w = 2; w < width - 2; w++, tBuf++, sBuf++)
			{
				byte4 rght2 = sBuf[-2];
				byte4 rght = sBuf[-1];
				byte4 cntr = sBuf[0];
				byte4 left = sBuf[1];
				byte4 left2 = sBuf[2];

				unsigned char R = (left2.R + left.R + cntr.R + rght.R + rght2.R) * 0.2f;
				unsigned char G = (left2.G + left.G + cntr.G + rght.G + rght2.G) * 0.2f;
				unsigned char B = (left2.B + left.B + cntr.B + rght.B + rght2.B) * 0.2f;

				tBuf[0] = byte4(R, G, B);
			}

			tBuf[0] = sBuf[0];
			tBuf[1] = sBuf[1];
		}

		//VERTICAL PASS
#pragma omp parallel for
		for (int w = 0; w < width; w++)
		{
			byte4* tBuf = TargetBuffer + w;
			byte4* sBuf = TempBuffer + w;

			tBuf[0] = sBuf[0];

			sBuf += width;
			tBuf += width;

			tBuf[0] = sBuf[0];
			sBuf += width;
			tBuf += width;

			for (int h = 2; h < height - 2; h++, tBuf += width, sBuf += width)
			{
				byte4 down2 = sBuf[-width2];
				byte4 down = sBuf[-width];
				byte4 cntr = sBuf[0];
				byte4 uppr = sBuf[width];
				byte4 uppr2 = sBuf[width2];


				unsigned char R = (down2.R + down.R + cntr.R + uppr.R + uppr2.R) * 0.2f;
				unsigned char G = (down2.G + down.G + cntr.G + uppr.G + uppr2.G) * 0.2f;
				unsigned char B = (down2.B + down.B + cntr.B + uppr.B + uppr2.B) * 0.2f;

				tBuf[0] = byte4(R, G, B);
			}

			tBuf[0] = sBuf[0];
			tBuf[width] = sBuf[width];
		}


	}


	__declspec(dllexport) void BOX_BLUR5_FLOAT(float* TargetBuffer, float* SourceBuffer, float* TempBuffer, long width, long height)
	{
		int width2 = width * 2;

		//HORIZONTAL PASS
#pragma omp parallel for
		for (int h = 0; h < height; h++)
		{
			float* tBuf = TempBuffer + h * width;
			float* sBuf = SourceBuffer + h * width;

			tBuf[0] = sBuf[0];
			tBuf++;
			sBuf++;
			tBuf[0] = sBuf[0];
			tBuf++;
			sBuf++;

			for (int w = 2; w < width - 2; w++, tBuf++, sBuf++)
			{
				float rght2 = sBuf[-2];
				float rght = sBuf[-1];
				float cntr = sBuf[0];
				float left = sBuf[1];
				float left2 = sBuf[2];

				tBuf[0] = (left2 + left + cntr + rght + rght2) * 0.2f;
			}

			tBuf[0] = sBuf[0];
			tBuf[1] = sBuf[1];
		}

		//VERTICAL PASS
#pragma omp parallel for
		for (int w = 0; w < width; w++)
		{
			float* tBuf = TargetBuffer + w;
			float* sBuf = TempBuffer + w;

			tBuf[0] = sBuf[0];

			sBuf += width;
			tBuf += width;

			tBuf[0] = sBuf[0];
			sBuf += width;
			tBuf += width;

			for (int h = 2; h < height - 2; h++, tBuf += width, sBuf += width)
			{
				float down2 = sBuf[-width2];
				float down = sBuf[-width];
				float cntr = sBuf[0];
				float uppr = sBuf[width];
				float uppr2 = sBuf[width2];


				tBuf[0] = (down2 + down + cntr + uppr + uppr2) * 0.2f;
			}

			tBuf[0] = sBuf[0];
			tBuf[width] = sBuf[width];
		}


	}


	__declspec(dllexport) void BLUR_MERGE(byte4* TargetBuffer, byte4* SourceBuffer, byte4* TempBuffer, long width, long height)
	{

	}
}