#include "stdafx.h"
#include <malloc.h>
#include <stdlib.h>
#include <atomic>
#include <omp.h>
#include "XFCore.h"
#include <math.h>
#include <ppl.h>
using namespace Concurrency;

#define M_PI 3.14159265358979323846f
#define RETURN_VALUE

inline void frtlzeromem(bool* dest, int count)
{
	for (int i = 0; i < count; ++i)
		dest[i] = false;
}

inline int WireframeDebug(int index, float* p, int* iptr, int iColor, int stride, int RW, int RH, vec3 ca, vec3 co, vec3 si,
	float nearZ, float farZ, float tanVert, float tanHorz, float rw, float rh, float fw, float fh, float oh, float ow, int s1)
{
	float* VERTEX_DATA = (float*)alloca(12 * 4);

	int BUFFER_SIZE = 3;

	for (int b = 0; b < 3; ++b)
	{
		float X = *(p + (index * s1 + b * stride)) - ca.x;
		float Y = *(p + (index * s1 + b * stride + 1)) - ca.y;
		float Z = *(p + (index * s1 + b * stride + 2)) - ca.z;

		float fiX = X * co.z - Z * si.z;
		float fiZ = Z * co.z + X * si.z;
		float ndY = Y * co.y + fiZ * si.y;

		//Returns the newly rotated Vector
		*(VERTEX_DATA + b * 3 + 0) = fiX * co.x - ndY * si.x;
		*(VERTEX_DATA + b * 3 + 1) = ndY * co.x + fiX * si.x;
		*(VERTEX_DATA + b * 3 + 2) = fiZ * co.y - Y * si.y;
	}
	//TODO: Replace RTL_ZERO_MEMORY with a simple loop, it should be much faster

	bool* AP = (bool*)alloca(BUFFER_SIZE + 12);
	//bool* AP = (bool*)(VERTEX_DATA + 48);

	//RtlZeroMemory(AP, BUFFER_SIZE);
	frtlzeromem(AP, BUFFER_SIZE);

#pragma region NearPlaneCFG

	int v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * 3 + 2] < nearZ)
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return -1;

#pragma endregion

#pragma region NearPlane
	if (v != 0)
	{
		float* strFLT = (float*)alloca(BUFFER_SIZE * 12 + 12);

		int API = 0;

		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
					API += 3;
				}
				else if (i > 0 && !AP[i - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
					API += 3;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * 3];
					strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
					API += 3;
				}
			}
		}

		BUFFER_SIZE = API / 3;
		VERTEX_DATA = strFLT;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}

#pragma endregion

#pragma region FarPlaneCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * 3 + 2] > farZ)
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return -1;

#pragma endregion

#pragma region FarPlane
	if (v != 0)
	{
		float* strFLT = (float*)alloca(BUFFER_SIZE * 12 + 12);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ);
					API += 3;
				}
				else if (i > 0 && !AP[i - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, i - 1, i, farZ);
					API += 3;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, i - 1, i, farZ);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * 3];
					strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
					API += 3;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / 3;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}
#pragma endregion

#pragma region RightFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * 3 + 2] * tanVert + ow < VERTEX_DATA[i * 3])
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return -1;
#pragma endregion

#pragma region RightFOV
	if (v != 0)
	{
		float* strFLT = (float*)alloca(BUFFER_SIZE * 12 + 12);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert, ow);
					API += 3;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, i - 1, i, tanVert, ow);
					API += 3;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert, ow);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, i - 1, i, tanVert, ow);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * 3];
					strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
					API += 3;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / 3;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}
#pragma endregion

#pragma region LeftFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * 3 + 2] * -tanVert - ow > VERTEX_DATA[i * 3])
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return -1;
#pragma endregion

#pragma region LeftFOV
	if (v != 0)
	{
		float* strFLT = (float*)alloca(BUFFER_SIZE * 12 + 12);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, -ow);
					API += 3;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, -ow);
					API += 3;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, -ow);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, -ow);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else
				{
					strFLT[API] = VERTEX_DATA[i * 3];
					strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
					API += 3;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / 3;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}
#pragma endregion

#pragma region TopFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * 3 + 2] * tanHorz + oh < VERTEX_DATA[i * 3 + 1])
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return -1;

#pragma endregion

#pragma region TopFOV

	if (v != 0)
	{
		float* strFLT = (float*)alloca(BUFFER_SIZE * 12 + 12);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz, oh);
					API += 3;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz, oh);
					API += 3;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz, oh);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz, oh);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * 3];
					strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
					API += 3;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / 3;
		RtlZeroMemory(AP, BUFFER_SIZE);


	}

#pragma endregion

#pragma region BottomFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * 3 + 2] * -tanHorz - oh > VERTEX_DATA[i * 3 + 1])
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return -1;

#pragma endregion

#pragma region BottomFOV
	if (v != 0)
	{
		float* strFLT = (float*)alloca(BUFFER_SIZE * 12 + 12);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, -oh);
					API += 3;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, -oh);
					API += 3;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, -oh);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, -oh);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * 3];
					strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
					API += 3;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / 3;
	}
#pragma endregion

	//if (LOG_T_COUNT) Interlocked.Increment(ref T_COUNT);

	//return 0;

	int PIXELC = 0;

	if (true) //matrixlerpv == 0
	{
		float r = 1.0f / VERTEX_DATA[2];
		float lX = rw + (VERTEX_DATA[0] * r) * fw;
		float lY = rh + (VERTEX_DATA[1] * r) * fh;

		if (lX < 0) lX = 0;
		if (lX >= RW) lX = RW - 1;
		if (lY < 0) lY = 0;
		if (lY >= RH) lY = RH - 1;

		float x = lX;
		float y = lY;

		for (int im = 1; im < BUFFER_SIZE; ++im) //replace 2 with BUFFER_SIZE
		{
			float r = 1.0f / VERTEX_DATA[im * 3 + 2];

			float posX = rw + (VERTEX_DATA[im * 3 + 0] * r) * fw;
			float posY = rh + (VERTEX_DATA[im * 3 + 1] * r) * fh;

			if (posX < 0) posX = 0;
			if (posX >= RW) posX = RW - 1;
			if (posY < 0) posY = 0;
			if (posY >= RH) posY = RH - 1;

			if (CountPixels) PIXELC += DrawLine(iptr, RW, iColor, posX, posY, lX, lY);
			else DrawLineFast(iptr, RW, iColor, posX, posY, lX, lY);

			lX = posX;
			lY = posY;
		}

		if (CountPixels) PIXELC += DrawLine(iptr, RW, iColor, x, y, lX, lY);
		else DrawLineFast(iptr, RW, iColor, x, y, lX, lY);
	}

	return PIXELC;
}


void FillDebug(int index, float* p, int* iptr, float* dptr, int iColor, int stride, int RW, int RH, vec3 ca, vec3 co, vec3 si,
	float nearZ, float farZ, float tanVert, float tanHorz, float rw, float rh, float fw, float fh, float oh, float ow, int s1, int FACE_CULL)
{
	float* VERTEX_DATA = (float*)alloca(12 * 4);

	int BUFFER_SIZE = 3;

	for (int b = 0; b < 3; ++b)
	{
		float X = *(p + (index * s1 + b * stride)) - ca.x;
		float Y = *(p + (index * s1 + b * stride + 1)) - ca.y;
		float Z = *(p + (index * s1 + b * stride + 2)) - ca.z;

		float fiX = X * co.z - Z * si.z;
		float fiZ = Z * co.z + X * si.z;
		float ndY = Y * co.y + fiZ * si.y;

		//Returns the newly rotated Vector
		*(VERTEX_DATA + b * 3 + 0) = fiX * co.x - ndY * si.x;
		*(VERTEX_DATA + b * 3 + 1) = ndY * co.x + fiX * si.x;
		*(VERTEX_DATA + b * 3 + 2) = fiZ * co.y - Y * si.y;
	}
	//TODO: Replace RTL_ZERO_MEMORY with a simple loop, it should be much faster

	bool* AP = (bool*)alloca(BUFFER_SIZE + 12);
	//bool* AP = (bool*)(VERTEX_DATA + 48);

	//RtlZeroMemory(AP, BUFFER_SIZE);
	frtlzeromem(AP, BUFFER_SIZE);

#pragma region NearPlaneCFG

	int v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * 3 + 2] < nearZ)
		{
			AP[i] = true;
			v++;
		}
	}

	//	OutputDebugString(L"\n");

	if (v == BUFFER_SIZE)
		return RETURN_VALUE;

#pragma endregion

#pragma region NearPlane
	if (v != 0)
	{
		float* strFLT = (float*)alloca(BUFFER_SIZE * 12 + 12);

		int API = 0;

		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
					API += 3;
				}
				else if (i > 0 && !AP[i - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
					API += 3;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * 3];
					strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
					API += 3;
				}
			}
		}

		BUFFER_SIZE = API / 3;
		VERTEX_DATA = strFLT;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}

#pragma endregion

#pragma region FarPlaneCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * 3 + 2] > farZ)
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
		float* strFLT = (float*)alloca(BUFFER_SIZE * 12 + 12);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ);
					API += 3;
				}
				else if (i > 0 && !AP[i - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, i - 1, i, farZ);
					API += 3;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					FIP(strFLT, API, VERTEX_DATA, i - 1, i, farZ);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * 3];
					strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
					API += 3;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / 3;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}
#pragma endregion

#pragma region RightFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * 3 + 2] * tanVert + ow < VERTEX_DATA[i * 3])
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
		float* strFLT = (float*)alloca(BUFFER_SIZE * 12 + 12);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert, ow);
					API += 3;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, i - 1, i, tanVert, ow);
					API += 3;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert, ow);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, i - 1, i, tanVert, ow);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * 3];
					strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
					API += 3;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / 3;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}
#pragma endregion

#pragma region LeftFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * 3 + 2] * -tanVert - ow > VERTEX_DATA[i * 3])
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
		float* strFLT = (float*)alloca(BUFFER_SIZE * 12 + 12);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, -ow);
					API += 3;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, -ow);
					API += 3;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, -ow);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIP(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, -ow);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else
				{
					strFLT[API] = VERTEX_DATA[i * 3];
					strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
					API += 3;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / 3;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}
#pragma endregion

#pragma region TopFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * 3 + 2] * tanHorz + oh < VERTEX_DATA[i * 3 + 1])
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
		float* strFLT = (float*)alloca(BUFFER_SIZE * 12 + 12);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz, oh);
					API += 3;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz, oh);
					API += 3;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz, oh);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz, oh);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * 3];
					strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
					API += 3;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / 3;
		RtlZeroMemory(AP, BUFFER_SIZE);


	}

#pragma endregion

#pragma region BottomFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * 3 + 2] * -tanHorz - oh > VERTEX_DATA[i * 3 + 1])
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
		float* strFLT = (float*)alloca(BUFFER_SIZE * 12 + 12);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, -oh);
					API += 3;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, -oh);
					API += 3;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, -oh);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPH(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, -oh);
					strFLT[API + 3] = VERTEX_DATA[i * 3];
					strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * 3];
					strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
					strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
					API += 3;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / 3;
	}
	#pragma endregion


	int PIXELC = 0;

	int yMax = 0;
	int yMin = RH;

	if (true)
		for (int im = 0; im < BUFFER_SIZE; im++)
		{
			VERTEX_DATA[im * 3 + 2] = 1.0f / VERTEX_DATA[im * 3 + 2];
			VERTEX_DATA[im * 3 + 0] = (rw + (VERTEX_DATA[im * 3 + 0] * VERTEX_DATA[im * 3 + 2]) * fw);
			VERTEX_DATA[im * 3 + 1] = (rh + (VERTEX_DATA[im * 3 + 1] * VERTEX_DATA[im * 3 + 2]) * fh);
			

		//	VERTEX_DATA[im * 3 + 1] = fabs((int)VERTEX_DATA[im * 3 + 1] - VERTEX_DATA[im * 3 + 1]) < 0.6f ? VERTEX_DATA[im * 3 + 1] : VERTEX_DATA[im * 3 + 1] + 1;

			//if (fabs(VERTEX_DATA[im * 3 + 1] - (RH - 1)) < 0.1) VERTEX_DATA[im * 3 + 1] = RH - 1;

			if (VERTEX_DATA[im * 3 + 1] > yMax) yMax = (int)(VERTEX_DATA[im * 3 + 1]);
			if (VERTEX_DATA[im * 3 + 1] < yMin) yMin = (int)(VERTEX_DATA[im * 3 + 1]);
		}

	if (FACE_CULL == 1 || FACE_CULL == 2)
	{
		float A = BACKFACECULL3(VERTEX_DATA);
		if (FACE_CULL == 2 && A > 0) return RETURN_VALUE;
		else if (FACE_CULL == 1 && A < 0) return RETURN_VALUE;
	}

	if (yMax >= RH) yMax = RH - 1;
	if (yMin < 0) yMin = 0;

	int BGR = 1948;
	//byte* bBGR = (byte*)&BGR;

	float slopeZ;
	float bZ;
	float s;

	float* Intersects = (float*)alloca(16);

	float* FROM;
	float* TO;

	int FromX;
	int ToX;

	int* RGB_iptr;
	float* Z_fptr;

	float zBegin;

	float oValue = 0.0f;


	for (int i = yMin; i <= yMax; ++i)
	{
		if (ScanLine(i, VERTEX_DATA, BUFFER_SIZE, Intersects))
		{
			if (Intersects[0] > Intersects[2])
			{
				TO = Intersects;
				FROM = Intersects + 2;
			}
			else
			{
				FROM = Intersects;
				TO = Intersects + 2;
			}

			FromX = (int)FROM[0] == 0 ? 0 : FROM[0] + 1;
			ToX = (int)TO[0];

			#pragma region Z_Interpolation
			slopeZ = (FROM[1] - TO[1]) / (FROM[0] - TO[0]);
			bZ = -slopeZ * FROM[0] + FROM[1];
			#pragma endregion

			#pragma region BufferOverflowProtection
			if (ToX >= RW) ToX = RW - 1;
			if (FromX < 0) FromX = 0;
			#pragma endregion

			RGB_iptr = iptr + i * RW;
			Z_fptr = dptr + i * RW;

			zBegin = slopeZ * (float)FromX + bZ;

			//if (CountPixels) PIXELC += (ToX - FromX + 1);
			
			for (int o = FromX; o <= ToX; ++o)
			{
				s = farZ - (1.0f / zBegin - oValue);
				zBegin += slopeZ;

				if (Z_fptr[o] > s) continue;

				//RGB_iptr++;
				Z_fptr[o] = s;
				RGB_iptr[o] = s * 2;				
			}

		}
	}
}


extern "C"
{
	DLL void WireframeDebug(int* iptr, float* p, long count, long stride, long iColor, vec3 co, vec3 si, vec3 ca, RenderSettings rconfig, long* P_Count, long* T_Count)
	{
		float radsFOV = rconfig.degFOV * M_PI / 180.0f;

		float nearZ = rconfig.nearZ, farZ = rconfig.farZ;
		float fovCoefficient = (float)tan((M_PI / 2.0f) - (radsFOV / 2.0f));
		float hFovCoefficient = ((float)rconfig.renderWidth / (float)rconfig.renderHeight) * (float)tan((M_PI / 2.0f) - (radsFOV / 2.0f));

		float tanVert = (float)tan((radsFOV / 2.0f)) * (1.0f - 0.0f);
		float tanHorz = (float)tan((radsFOV) / 2.0f) * ((float)rconfig.renderHeight / (float)rconfig.renderWidth) * (1.0f - 0.0f);

		float rw = (rconfig.renderWidth - 1.0f) / 2.0f, rh = (rconfig.renderHeight - 1.0f) / 2.0f;

		float fw = rw * fovCoefficient;
		float fh = rh * hFovCoefficient;

		float oh = 0;
		float ow = 0;

		int s1 = stride * 3;

		int PixelWriteCount = 0;
		int TrisCount = 0;

		//	std::atomic<int> PixelWriteCount;
		//	std::atomic<int> TrisCount;

		int* ptrze = 0;
		bool enableLog = CountPixels || CountTriangles;

		if (enableLog)
		{
			ptrze = (int*)malloc(4 * count);
			RtlZeroMemory(ptrze, 4 * count);
		}

		if (!ForceUseOpenMP)
		{
			int cnt = count;
			parallel_for(0, cnt, [&](int index)
			{
				int pf = WireframeDebug(index, p, iptr, iColor, stride, rconfig.renderWidth, rconfig.renderHeight, ca, co, si, nearZ, farZ, tanVert, tanHorz, rw, rh, fw, fh, oh, ow, s1);

				if (enableLog && pf != -1) ptrze[index] = pf;
			});
		}
		else
		{
#pragma omp parallel for num_threads(4)
			for (int index = 0; index < count; ++index)
			{
				int pf = WireframeDebug(index, p, iptr, iColor, stride, rconfig.renderWidth, rconfig.renderHeight, ca, co, si, nearZ, farZ, tanVert, tanHorz, rw, rh, fw, fh, oh, ow, s1);

				if (enableLog && pf != -1) ptrze[index] = pf;
			}
		}

		//having a second loop improves performance
		if (ptrze)
			for (int i = 0; i < count; ++i)
				if (ptrze[i] != 0)
				{
					if (CountPixels) PixelWriteCount += ptrze[i];
					if (CountTriangles) ++TrisCount;
				}

		if (CountPixels || CountTriangles) free(ptrze);

		*P_Count = PixelWriteCount;
		*T_Count = TrisCount;
	}

	DLL void FillFlatDebug(int* iptr, float* dptr, float* p, long count, long stride, long iColor, vec3 co, vec3 si, vec3 ca, RenderSettings rconfig, long FC, long* P_Count, long* T_Count)
	{
		float radsFOV = rconfig.degFOV * M_PI / 180.0f;

		float nearZ = rconfig.nearZ, farZ = rconfig.farZ;
		float fovCoefficient = (float)tan((M_PI / 2.0f) - (radsFOV / 2.0f));
		float hFovCoefficient = ((float)rconfig.renderWidth / (float)rconfig.renderHeight) * (float)tan((M_PI / 2.0f) - (radsFOV / 2.0f));

		float tanVert = (float)tan((radsFOV / 2.0f)) * (1.0f - 0.0f);
		float tanHorz = (float)tan((radsFOV) / 2.0f) * ((float)rconfig.renderHeight / (float)rconfig.renderWidth) * (1.0f - 0.0f);

		float rw = (rconfig.renderWidth - 1.0f) / 2.0f, rh = (rconfig.renderHeight - 1.0f) / 2.0f;

		float fw = rw * fovCoefficient;
		float fh = rh * hFovCoefficient;

		float oh = 0;
		float ow = 0;

		int s1 = stride * 3;

		int* ptrze = 0;
		bool enableLog = CountPixels || CountTriangles;

		if (enableLog)
		{
			ptrze = (int*)malloc(4 * count);
			RtlZeroMemory(ptrze, 4 * count);
		}


		int PixelWriteCount = 0;
		int TrisCount = 0;

		int cnt = count;

		

	//	parallel_for(0, cnt, [&](int index)
	//	{
	//		FillDebug(index, p, iptr, dptr, iColor, stride, rconfig.renderWidth, rconfig.renderHeight, ca, co, si, nearZ, farZ, tanVert, tanHorz, rw, rh, fw, fh, oh, ow, s1, FC);
	//	});
		

		
#pragma omp parallel for num_threads(4)
		for (int index = 0; index < cnt; ++index)
		{
			FillDebug(index, p, iptr, dptr, iColor, stride, rconfig.renderWidth, rconfig.renderHeight, ca, co, si, nearZ, farZ, tanVert, tanHorz, rw, rh, fw, fh, oh, ow, s1, FC);
		}
		


		if (enableLog)
			for (int i = 0; i < count; ++i)
				if (ptrze[i] != 0)
				{
					if (CountPixels) PixelWriteCount += ptrze[i];
					if (CountTriangles) ++TrisCount;
				}

		if (CountPixels || CountTriangles) free(ptrze);


		*P_Count = PixelWriteCount;
		*T_Count = TrisCount;
	}
}
