#include "stdafx.h"
#include <malloc.h>
#include <stdlib.h>
#include <atomic>
#include <omp.h>
#include "XFCore.h"
#include <math.h>
#include <ppl.h>

using namespace Concurrency;
#include <iostream>

#define M_PI 3.14159265358979323846f
#define RETURN_VALUE

struct PhongConfig
{
	vec3 lightPosition;
	vec3 lightColor;
	vec3 objectColor;

	float ambientStrength;
	float specularStrength;
	long specularPower;

	long shadowMapPresent; //0 No 1 Yes
	float* shadowMapAddress;
	long shadowMapWidth;
	long shadowMapHeight;

	float srw;
	float srh;

	float sfw;
	float sfh;

	vec3 lightPosReal;
	vec3 lightRotCos;
	vec3 lightRotSin;

	float ShadowBias;
	float ShadowNormalBias;
};

struct int2
{
	int X;
	int Y;

	int2(int x, int y)
	{
		X = x;
		Y = y;
	}
};

inline float textureBILINEAR(PhongConfig inputTexture, vec3 coord)
{
	float x1 = coord.x + 0.5f - (int)(coord.x + 0.5f);
	float x0 = 1.0f - x1;

	float y1 = coord.y + 0.5f - (int)(coord.y + 0.5f);
	float y0 = 1.0f - y1;

	int2 X0Y0 = int2((int)(coord.x - 0.5) + 0.5, (int)(coord.y - 0.5f) + 0.5f);
	int2 X1Y0 = int2((int)(coord.x + 0.5) + 0.5, (int)(coord.y - 0.5f) + 0.5f);

	int2 X0Y1 = int2((int)(coord.x - 0.5) + 0.5, (int)(coord.y + 0.5f) + 0.5f);
	int2 X1Y1 = int2((int)(coord.x + 0.5) + 0.5, (int)(coord.y + 0.5f) + 0.5f);

	// -> if statement checking for bounds <-
	if (true) //TEXTURE_WRAP_MODE == 0
	{
		if (X0Y0.X < 0) X0Y0.X = 0;
		if (X0Y0.Y < 0) X0Y0.Y = 0;
		if (X0Y0.X >= inputTexture.shadowMapWidth) X0Y0.X = inputTexture.shadowMapWidth - 1;
		if (X0Y0.Y >= inputTexture.shadowMapHeight) X0Y0.Y = inputTexture.shadowMapHeight - 1;

		if (X1Y0.X < 0) X1Y0.X = 0;
		if (X1Y0.Y < 0) X1Y0.Y = 0;
		if (X1Y0.X >= inputTexture.shadowMapWidth) X1Y0.X = inputTexture.shadowMapWidth - 1;
		if (X1Y0.Y >= inputTexture.shadowMapHeight) X1Y0.Y = inputTexture.shadowMapHeight - 1;

		if (X0Y1.X < 0) X0Y1.X = 0;
		if (X0Y1.Y < 0) X0Y1.Y = 0;
		if (X0Y1.X >= inputTexture.shadowMapWidth) X0Y1.X = inputTexture.shadowMapWidth - 1;
		if (X0Y1.Y >= inputTexture.shadowMapHeight) X0Y1.Y = inputTexture.shadowMapHeight - 1;

		if (X1Y1.X < 0) X1Y1.X = 0;
		if (X1Y1.Y < 0) X1Y1.Y = 0;
		if (X1Y1.X >= inputTexture.shadowMapWidth) X1Y1.X = inputTexture.shadowMapWidth - 1;
		if (X1Y1.Y >= inputTexture.shadowMapHeight) X1Y1.Y = inputTexture.shadowMapHeight - 1;
	}


	float bptrL = *(inputTexture.shadowMapAddress + X0Y0.Y * inputTexture.shadowMapWidth + X0Y0.X);
	float bptrLN = *(inputTexture.shadowMapAddress + X1Y0.Y * inputTexture.shadowMapWidth + X1Y0.X);

	float bptrU = *(inputTexture.shadowMapAddress + X0Y1.Y * inputTexture.shadowMapWidth + X0Y1.X);
	float bptrUN = *(inputTexture.shadowMapAddress + X1Y1.Y * inputTexture.shadowMapWidth + X1Y1.X);

	return bptrL * (x0 * y0) + bptrLN * (x1 * y0) + bptrU * (x0 * y1) + bptrUN * (x1 * y1);
}


inline float SampleShadow(int iX, int iY, PhongConfig pc, float DEPTH)
{
	if (iX >= 0 && iY >= 0 && iX < pc.shadowMapWidth && iY < pc.shadowMapHeight)
	{
		float sampleDepth = pc.shadowMapAddress[iX + iY * pc.shadowMapWidth];

		if (DEPTH > (sampleDepth - pc.ShadowBias))
		{
			return 1.0f;
		}
		else
		{
			return 0.05f;
		}
	}
	else
	{
		return 0.05f;
	}
}

inline float SampleShadowDelta(int iX, int iY, PhongConfig pc, float DEPTH)
{
	if (iX >= 0 && iY >= 0 && iX < pc.shadowMapWidth && iY < pc.shadowMapHeight)
	{
		float sampleDepth = pc.shadowMapAddress[iX + iY * pc.shadowMapWidth];

		return DEPTH - (sampleDepth - pc.ShadowBias);
	}
	else
	{
		return 0.5f;
	}
}

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


void FillPhong2(int index, float* p, int* iptr, float* dptr, int stride, int RW, int RH, vec3 ca, vec3 co, vec3 si,
	float nearZ, float farZ, float tanVert, float tanHorz, float rw, float rh, float fw, float fh, float oh, float ow, int s1, int FACE_CULL, PhongConfig pc)
{
	float* VERTEX_DATA = (float*)alloca(stride * 3 * 4);

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
		*(VERTEX_DATA + b * stride + 0) = fiX * co.x - ndY * si.x;
		*(VERTEX_DATA + b * stride + 1) = ndY * co.x + fiX * si.x;
		*(VERTEX_DATA + b * stride + 2) = fiZ * co.y - Y * si.y;

		for (int a = 3; a < stride; a++)
			VERTEX_DATA[b * stride + a] = *(p + (index * s1) + b * stride + a);
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
		if (VERTEX_DATA[i * stride + 2] < nearZ)
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
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, nearZ, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ, stride);
					strFLT[API + 3] = VERTEX_DATA[i * stride];
					strFLT[API + 4] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 5] = VERTEX_DATA[i * stride + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, nearZ, stride);
					strFLT[API + 3] = VERTEX_DATA[i * stride];
					strFLT[API + 4] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 5] = VERTEX_DATA[i * stride + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];
					API += stride;
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
		if (VERTEX_DATA[i * stride + 2] > farZ)
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
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, farZ, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ, stride);
					strFLT[API + 3] = VERTEX_DATA[i * stride];
					strFLT[API + 4] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 5] = VERTEX_DATA[i * stride + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, farZ, stride);
					strFLT[API + 3] = VERTEX_DATA[i * stride];
					strFLT[API + 4] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 5] = VERTEX_DATA[i * stride + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];
					API += stride;
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
		if (VERTEX_DATA[i * stride + 2] * tanVert + ow < VERTEX_DATA[i * stride])
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
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert, ow, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, tanVert, ow, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert, ow, stride);
					strFLT[API + 3] = VERTEX_DATA[i * stride];
					strFLT[API + 4] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 5] = VERTEX_DATA[i * stride + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, tanVert, ow, stride);
					strFLT[API + 3] = VERTEX_DATA[i * stride];
					strFLT[API + 4] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 5] = VERTEX_DATA[i * stride + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];
					API += stride;
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
		if (VERTEX_DATA[i * stride + 2] * -tanVert - ow > VERTEX_DATA[i * stride])
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
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, -ow, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, -ow, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, -ow, stride);
					strFLT[API + 3] = VERTEX_DATA[i * stride];
					strFLT[API + 4] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 5] = VERTEX_DATA[i * stride + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, -ow, stride);
					strFLT[API + 3] = VERTEX_DATA[i * stride];
					strFLT[API + 4] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 5] = VERTEX_DATA[i * stride + 2];
					API += 6;
				}
				else
				{
					strFLT[API] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];
					API += stride;
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
		if (VERTEX_DATA[i * stride + 2] * tanHorz + oh < VERTEX_DATA[i * stride + 1])
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
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz, oh, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz, oh, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz, oh, stride);
					strFLT[API + 3] = VERTEX_DATA[i * stride];
					strFLT[API + 4] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 5] = VERTEX_DATA[i * stride + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz, oh, stride);
					strFLT[API + 3] = VERTEX_DATA[i * stride];
					strFLT[API + 4] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 5] = VERTEX_DATA[i * stride + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];
					API += stride;
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
		if (VERTEX_DATA[i * stride + 2] * -tanHorz - oh > VERTEX_DATA[i * stride + 1])
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
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, -oh, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, -oh, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, -oh, stride);
					strFLT[API + 3] = VERTEX_DATA[i * stride];
					strFLT[API + 4] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 5] = VERTEX_DATA[i * stride + 2];
					API += 6;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, -oh, stride);
					strFLT[API + 3] = VERTEX_DATA[i * stride];
					strFLT[API + 4] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 5] = VERTEX_DATA[i * stride + 2];
					API += 6;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];
					API += stride;
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
			VERTEX_DATA[im * stride + 2] = 1.0f / VERTEX_DATA[im * stride + 2];
			VERTEX_DATA[im * stride + 0] = (rw + (VERTEX_DATA[im * stride + 0] * VERTEX_DATA[im * stride + 2]) * fw);
			VERTEX_DATA[im * stride + 1] = (rh + (VERTEX_DATA[im * stride + 1] * VERTEX_DATA[im * stride + 2]) * fh);

			if (VERTEX_DATA[im * stride + 1] > yMax) yMax = (int)(VERTEX_DATA[im * stride + 1]);
			if (VERTEX_DATA[im * stride + 1] < yMin) yMin = (int)(VERTEX_DATA[im * stride + 1]);
		}

	if (FACE_CULL == 1 || FACE_CULL == 2)
	{
		float A = BACKFACECULLS(VERTEX_DATA, stride);
		if (FACE_CULL == 2 && A > 0) return RETURN_VALUE;
		else if (FACE_CULL == 1 && A < 0) return RETURN_VALUE;
	}

	if (yMax >= RH) yMax = RH - 1;
	if (yMin < 0) yMin = 0;



	float slopeZ;
	float bZ;
	float s;

	float sA;
	float sB;

	//float* Intersects = (float*)alloca(16);
	float* Intersects = (float*)alloca((4 + (stride - 3) * 5) * 4);
	float* az = Intersects + 4 + (stride - 3) * 2;
	float* slopeAstack = az + (stride - 3) + 0;
	float* bAstack = slopeAstack + (stride - 3);



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

			float ZDIFF = 1.0f / FROM[1] - 1.0f / TO[1];
			bool usingZ = ZDIFF != 0;
			if (ZDIFF != 0) usingZ = ZDIFF * ZDIFF >= 0.00001f;

			if (usingZ)
				for (int b = 0; b < stride - 3; b++)
				{
					sA = (FROM[2 + b] - TO[2 + b]) / ZDIFF;
					sB = -sA / FROM[1] + FROM[2 + b];

					slopeAstack[b] = sA;
					bAstack[b] = sB;
				}
			else
			for (int b = 0; b < stride - 3; b++)
				{
					sA = (FROM[2 + b] - TO[2 + b]) / (FROM[0] - TO[0]);
					sB = -sA * FROM[0] + FROM[2 + b];

					slopeAstack[b] = sA;
					bAstack[b] = sB;
				}

			RGB_iptr = iptr + i * RW;
			Z_fptr = dptr + i * RW;

			zBegin = slopeZ * (float)FromX + bZ;

			vec3 Normal;
			vec3 FragPos;

			for (int o = FromX; o <= ToX; ++o)
			{
				s = farZ - (1.0f / zBegin - oValue);
				zBegin += slopeZ;

				if (Z_fptr[o] > s) continue;
				Z_fptr[o] = s;

				//Normal = 
				//New Scope->
				if (false)
				{
					float ambientStrength = 0.1;
					vec3 ambient = pc.lightColor * ambientStrength;

					// diffuse 
					vec3 norm = normalize(Normal);
					vec3 lightDir = normalize(pc.lightPosition - FragPos);
					float diff = max(dot(norm, lightDir), 0.0);
					vec3 diffuse = pc.lightColor * diff;

					// specular
					float specularStrength = 0.5;
					vec3 viewDir = normalize(-FragPos); // the viewer is always at (0,0,0) in view-space, so viewDir is (0,0,0) - Position => -Position
					vec3 reflectDir = reflect(-lightDir, norm);
					float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
					vec3 specular = pc.lightColor * specularStrength * spec;

					vec3 result = pc.objectColor * (ambient + diffuse + specular);
					unsigned char* bptr = (unsigned char*)(RGB_iptr + o);
					bptr[0] = result.z * 255.0;
					bptr[1] = result.z * 255.0;
					bptr[2] = result.z * 255.0;
				}
				RGB_iptr[o] = 2 * s;

			}

		}
	}
}

void FillPhong(int index, float* p, int* iptr, float* dptr, int stride, int RW, int RH, vec3 ca, vec3 co, vec3 si,
	float nearZ, float farZ, float tanVert, float tanHorz, float rw, float rh, float fw, float fh, float oh, float ow, int s1, int FACE_CULL, PhongConfig pc)
{
	int readStride = stride;
	stride = pc.shadowMapPresent == 1 ? 9 : 6;

	float* VERTEX_DATA = (float*)alloca(stride * 3 * 4);

	int BUFFER_SIZE = 3;

	

	bool shadowsEnabled = pc.shadowMapPresent == 1 ? true : false;

	for (int b = 0; b < 3; ++b)
	{
		float X = *(p + (index * s1 + b * readStride)) - ca.x;
		float Y = *(p + (index * s1 + b * readStride + 1)) - ca.y;
		float Z = *(p + (index * s1 + b * readStride + 2)) - ca.z;

		float fiX = X * co.z - Z * si.z;
		float fiZ = Z * co.z + X * si.z;
		float ndY = Y * co.y + fiZ * si.y;

		//Returns the newly rotated Vector
		*(VERTEX_DATA + b * stride + 0) = fiX * co.x - ndY * si.x;
		*(VERTEX_DATA + b * stride + 1) = ndY * co.x + fiX * si.x;
		*(VERTEX_DATA + b * stride + 2) = fiZ * co.y - Y * si.y;

		for (int a = 3; a < stride; a++)
			VERTEX_DATA[b * stride + a] = *(p + (index * s1) + b * readStride + a);

		//XYPosition->
		if (shadowsEnabled)
		{
			ToCameraSpace(p + (index * s1 + b * readStride), VERTEX_DATA + b * stride + 6, pc.lightPosReal, pc.lightRotCos, pc.lightRotSin);
		}

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
		if (VERTEX_DATA[i * stride + 2] < nearZ)
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
		float* strFLT = (float*)alloca((BUFFER_SIZE * stride + stride) * 4);

		int API = 0;

		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, nearZ, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ, stride);

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
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, nearZ, stride);
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
		if (VERTEX_DATA[i * stride + 2] > farZ)
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
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, farZ, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ, stride);
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
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, farZ, stride);
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
		if (VERTEX_DATA[i * stride + 2] * tanVert + ow < VERTEX_DATA[i * stride])
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
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert, ow, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, tanVert, ow, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert, ow, stride);
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
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, tanVert, ow, stride);
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
		if (VERTEX_DATA[i * stride + 2] * -tanVert - ow > VERTEX_DATA[i * stride])
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
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, -ow, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, -ow, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, -ow, stride);
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
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, -ow, stride);
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
		if (VERTEX_DATA[i * stride + 2] * tanHorz + oh < VERTEX_DATA[i * stride + 1])
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
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz, oh, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz, oh, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz, oh, stride);
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
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz, oh, stride);
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
		if (VERTEX_DATA[i * stride + 2] * -tanHorz - oh > VERTEX_DATA[i * stride + 1])
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
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, -oh, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, -oh, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, -oh, stride);
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
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, -oh, stride);
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

	int PIXELC = 0;

	int yMax = 0;
	int yMin = RH;

	if (true)
	for (int im = 0; im < BUFFER_SIZE; im++)
	{
		VERTEX_DATA[im * stride + 2] = 1.0f / VERTEX_DATA[im * stride + 2];
		VERTEX_DATA[im * stride + 0] = (rw + (VERTEX_DATA[im * stride + 0] * VERTEX_DATA[im * stride + 2]) * fw);
		VERTEX_DATA[im * stride + 1] = (rh + (VERTEX_DATA[im * stride + 1] * VERTEX_DATA[im * stride + 2]) * fh);

		if (VERTEX_DATA[im * stride + 1] > yMax) yMax = (int)(VERTEX_DATA[im * stride + 1]);
		if (VERTEX_DATA[im * stride + 1] < yMin) yMin = (int)(VERTEX_DATA[im * stride + 1]);
	}
	
	if (false)
	for (int im = 0; im < 1; im++)
	{
		int posX = VERTEX_DATA[im * stride + 0];
		int posY = VERTEX_DATA[im * stride + 1];

		if (posX < 0) posX = 0;
		if (posX >= RW) posX = RW - 1;
		if (posY < 0) posY = 0;
		if (posY >= RH) posY = RH - 1;

		iptr[posX + posY * RW] = FastInt(VERTEX_DATA[im * stride + 3] * 127.5f + 127.5f, VERTEX_DATA[im * stride + 4] * 127.5f + 127.5f, VERTEX_DATA[im * stride + 5] * 127.5f + 127.5f);
	}


	
	if (FACE_CULL == 1 || FACE_CULL == 2)
	{
		float A = BACKFACECULLS(VERTEX_DATA, stride);
		if (FACE_CULL == 2 && A > 0) return RETURN_VALUE;
		else if (FACE_CULL == 1 && A < 0) return RETURN_VALUE;
	}
	if (yMax >= RH) yMax = RH - 1;
	if (yMin < 0) yMin = 0;

	float slopeZ;
	float bZ;
	float s;

	float sA;
	float sB;

	//float* Intersects = (float*)alloca(16);
	float* Intersects = (float*)alloca((4 + (stride - 3) * 5) * 4);
	float* az = Intersects + 4 + (stride - 3) * 2;
	float* slopeAstack = az + (stride - 3) + 0;
	float* bAstack = slopeAstack + (stride - 3);

	float NormalBias = pc.ShadowNormalBias;
	float ambientStrength = pc.ambientStrength;
	float specularStrength = pc.specularStrength;

	float* FROM;
	float* TO;

	int FromX;
	int ToX;

	int* RGB_iptr;
	float* Z_fptr;

	float zBegin;

	float oValue = 0.0f;

	float fwi = 1.0f / fw;
	float fhi = 1.0f / fh;

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

			FromX = (int)FROM[0] == 0 ? 0 : FROM[0] + 1;
			ToX = (int)TO[0];

		//	*(iptr + i * RW + FromX) = FastInt(FROM[2] * 127.5f + 127.5f, FROM[3] * 127.5f + 127.5f, FROM[4] * 127.5f + 127.5f);
		//	*(iptr + i * RW + ToX) = FastInt(TO[2] * 127.5f + 127.5f, TO[3] * 127.5f + 127.5f, TO[4] * 127.5f + 127.5f);

		//	continue;
#pragma region Z_Interpolation
			slopeZ = (FROM[1] - TO[1]) / (FROM[0] - TO[0]);
			bZ = -slopeZ * FROM[0] + FROM[1];
#pragma endregion

#pragma region BufferOverflowProtection
			if (ToX >= RW) ToX = RW - 1;
			if (FromX < 0) FromX = 0;
#pragma endregion

			float ZDIFF = 1.0f / FROM[1] - 1.0f / TO[1];
			bool usingZ = ZDIFF != 0;
			if (ZDIFF != 0) usingZ = ZDIFF * ZDIFF >= 0.005f;

			if (usingZ)
				for (int b = 0; b < stride - 3; b++)
				{
					sA = (FROM[2 + b] - TO[2 + b]) / ZDIFF;
					sB = -sA / FROM[1] + FROM[2 + b];

					slopeAstack[b] = sA;
					bAstack[b] = sB;
				}
			else
				for (int b = 0; b < stride - 3; b++)
				{
					sA = (FROM[2 + b] - TO[2 + b]) / (FROM[0] - TO[0]);
					sB = -sA * FROM[0] + FROM[2 + b];

					slopeAstack[b] = sA;
					bAstack[b] = sB;
				}

			RGB_iptr = iptr + i * RW;
			Z_fptr = dptr + i * RW;

			zBegin = slopeZ * (float)FromX + bZ;

			vec3 Normal;
			vec3 FragPos;

			

			float yPosZ = (i - rh) * fhi;

			for (int o = FromX; o <= ToX; ++o)
			{
				float depth = (1.0f / zBegin - oValue);
				s = farZ - depth;
				zBegin += slopeZ;

				if (Z_fptr[o] > s) continue;
				Z_fptr[o] = s;

				if (usingZ) for (int z = 0; z < stride - 3; z++) az[z] = (slopeAstack[z] * depth + bAstack[z]);
				else for (int z = 0; z < stride - 3; z++) az[z] = (slopeAstack[z] * (float)o + bAstack[z]);

				//Normal = vec3(az[0], az[1], az[2]);
				FragPos = vec3((o - rw) * fwi * depth, yPosZ * depth, depth);

				Normal = ToCameraSpace(vec3(az[0], az[1], az[2]), co, si);
				vec3 shadowNormal = ToCameraSpace(vec3(az[0],az[1],az[2]), pc.lightRotCos, pc.lightRotSin);
				//RGB_iptr[o] = FastInt(az[0] * 127.5f + 127.5f, az[1] * 127.5f + 127.5f, az[2] * 127.5f + 127.5f);

				float ShadowMult = 1.0f;

				if (shadowsEnabled)
				{
					ShadowMult = 0;

					float nBias = NormalBias;

					float X;
					float Y;

					float XTrue;
					float YTrue;

					vec3 zero = vec3(0, 0, 1);
					float dotp = dot(shadowNormal, zero);

					if (dotp > 1.0f) dotp = 1.0f;
					else if (dotp < -1.0f) dotp = -1.0f;

					nBias *= (-fabsf(dotp) + 1.0f);

					vec3 resl = vec3(az[3] + shadowNormal.x * nBias, az[4] + shadowNormal.y * nBias, az[5] - shadowNormal.z * nBias);
			

					ToXY(resl, pc.srw, pc.srh, pc.sfw, pc.sfh, &X, &Y);

					ToXY(vec3(az[3], az[4], az[5]), pc.srw, pc.srh, pc.sfw, pc.sfh, &XTrue, &YTrue);

					//az[5] = farZ - (az[5] + shadowNormal.z * nBias);
					az[5] = farZ - az[5];

					int iX = (int)X;
					int iY = (int)Y;

					//Sample Smart ->

					float xhgh = X - iX;
					float xlwr = 1.0f - xhgh;

					float yhgh = Y - iY;
					float ylwr = 1.0f - yhgh;
					

				//	ShadowMult = (SampleShadow(iX, iY, pc, az[5]) * xlwr + SampleShadow(iX + 1, iY, pc, az[5]) * xhgh) * ylwr +
				//		(SampleShadow(iX, iY + 1, pc, az[5]) * xlwr + SampleShadow(iX + 1, iY + 1, pc, az[5]) * xhgh) * yhgh;


					if (true)
					{
						float sampleDepth;
					//	sampleDepth = textureBILINEAR(pc, vec3(X - 0.5f, Y - 0.5f, 0));
					//	sampleDepth += textureBILINEAR(pc, vec3(X - 0.5f, Y + 0.5f, 0));
					//	sampleDepth += textureBILINEAR(pc, vec3(X + 0.5f, Y + 0.5f, 0));
					//	sampleDepth += textureBILINEAR(pc, vec3(X + 0.5f, Y - 0.5f, 0));
						sampleDepth = textureBILINEAR(pc, vec3(X, Y, 0));

						//sampleDepth *= 0.20f;

						float deltaV = az[5] - (sampleDepth - pc.ShadowBias);

						if (deltaV > 0)
						{
							ShadowMult = 1.0f;
						}
						else
						{
							ShadowMult = 0.05f;
						}

					//	float v = SampleShadowDelta(XTrue, YTrue, pc, az[5]);

						float ab = -deltaV;

						//ShadowMult = ab < 0 ? 0.3f : 1.0f;

						//if (false)
						if (ab <= 5.0f && ab >= 0.0f)
						{
							//ShadowMult *= ab;// *0.2f;
							ShadowMult = (5.0f - ab) * 0.2f;
						}

						//ShadowMult *= (1.0f - nBias);

					}

					if (false)
					{
					
						ShadowMult += SampleShadow(iX, iY, pc, az[5]);
						ShadowMult += SampleShadow(iX + 1, iY, pc, az[5]);
						ShadowMult += SampleShadow(iX + 1, iY + 1, pc, az[5]);
						ShadowMult += SampleShadow(iX, iY + 1, pc, az[5]);

						ShadowMult *= 0.25;
					}
					

					/*
					if (iX >= 0 && iY >= 0 && iX < pc.shadowMapWidth && iY < pc.shadowMapHeight)
					{
						float sampleDepth = pc.shadowMapAddress[iX + iY * pc.shadowMapWidth];

						if (az[5] > (sampleDepth - pc.ShadowBias))
							ShadowMult = 1.0f;
						else
							ShadowMult = 0.25f;
					}
					else
						ShadowMult = 0.25f;
					*/
					
				}

				RGB_iptr[o] = FastInt(ShadowMult * (az[0] * 127.5f + 127.5f), ShadowMult * (az[1] * 127.5f + 127.5f), ShadowMult * (az[2] * 127.5f + 127.5f));

			//	RGB_iptr[o] = FastInt(Normal.z * 127.5f + 127.5f, Normal.y * 127.5f + 127.5f, Normal.x * 127.5f + 127.5f);

				//Normal = 
				//New Scope->
				if (false)
				{
					vec3 ambient = pc.lightColor * ambientStrength;

					// diffuse 
					//vec3 norm = normalize(Normal);
					vec3 norm = Normal;

					vec3 lightDir = normalize(pc.lightPosition - FragPos);
					float diff = max(dot(norm, lightDir), 0.0);
					vec3 diffuse = pc.lightColor * diff;

					// specular
					vec3 specular = vec3(0, 0, 0);

					if (specularStrength > 0)
					{
						vec3 viewDir = normalize(-FragPos); // the viewer is always at (0,0,0) in view-space, so viewDir is (0,0,0) - Position => -Position
						vec3 reflectDir = reflect(-lightDir, norm);
						float spec = pow(max(dot(viewDir, reflectDir), 0.0), pc.specularPower);
						specular = pc.lightColor * specularStrength * spec;
					}

					vec3 result = pc.objectColor * (ambient + diffuse + specular);

					if (ShadowMult != 1)
					{
						result = result * ShadowMult;
					}

					result.Clamp01();

					//if (Z_fptr[o] > s) continue;
					RGB_iptr[o] = FastInt(result.z * 255.0f, result.y * 255.0f, result.x * 255.0f);

				}
				//RGB_iptr[o] = 2.0f * s;
				//RGB_iptr[o] = 1239842;
			}
		}
	}
}

void FillDepth(int index, float* p, float* dptr, int stride, int RW, int RH, vec3 ca, vec3 co, vec3 si,
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


	int yMax = 0;
	int yMin = RH;


	for (int im = 0; im < BUFFER_SIZE; im++)
	{
		VERTEX_DATA[im * 3 + 2] = 1.0f / VERTEX_DATA[im * 3 + 2];
		VERTEX_DATA[im * 3 + 0] = (rw + (VERTEX_DATA[im * 3 + 0] * VERTEX_DATA[im * 3 + 2]) * fw);
		VERTEX_DATA[im * 3 + 1] = (rh + (VERTEX_DATA[im * 3 + 1] * VERTEX_DATA[im * 3 + 2]) * fh);

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


	float slopeZ, bZ, s;

	float* Intersects = (float*)alloca(16);

	float* FROM;
	float* TO;

	int FromX;
	int ToX;

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

			Z_fptr = dptr + i * RW;
			zBegin = slopeZ * (float)FromX + bZ;

			for (int o = FromX; o <= ToX; ++o)
			{
				s = farZ - (1.0f / zBegin - oValue);
				zBegin += slopeZ;

				if (Z_fptr[o] > s) continue;
				Z_fptr[o] = s;
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

		

		parallel_for(0, cnt, [&](int index)
		{
			FillDebug(index, p, iptr, dptr, iColor, stride, rconfig.renderWidth, rconfig.renderHeight, ca, co, si, nearZ, farZ, tanVert, tanHorz, rw, rh, fw, fh, oh, ow, s1, FC);
		});
		

		/*
#pragma omp parallel for num_threads(4)
		for (int index = 0; index < cnt; ++index)
		{
			FillDebug(index, p, iptr, dptr, iColor, stride, rconfig.renderWidth, rconfig.renderHeight, ca, co, si, nearZ, farZ, tanVert, tanHorz, rw, rh, fw, fh, oh, ow, s1, FC);
		}
		*/


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

	DLL void PhongBase(int* iptr, float* dptr, float* p, long count, long stride, vec3 co, vec3 si, vec3 ca, RenderSettings rconfig, PhongConfig pc, long FC)
	{
		float radsFOV = rconfig.degFOV * M_PI / 180.0f;

		float nearZ = rconfig.nearZ, farZ = rconfig.farZ;
		float fovCoefficient = (float)tan((M_PI / 2.0f) - (radsFOV / 2.0f));
		float hFovCoefficient = ((float)rconfig.renderWidth / (float)rconfig.renderHeight) * (float)tan((M_PI / 2.0f) - (radsFOV / 2.0f));

		float tanVert = (float)tan(radsFOV / 2.0f) * (1.0f - 0.0f);
		float tanHorz = (float)tan(radsFOV / 2.0f) * ((float)rconfig.renderHeight / (float)rconfig.renderWidth) * (1.0f - 0.0f);

		float rw = (rconfig.renderWidth - 1.0f) / 2.0f, rh = (rconfig.renderHeight - 1.0f) / 2.0f;

		float fw = rw * fovCoefficient;
		float fh = rh * hFovCoefficient;

		float oh = 0;
		float ow = 0;

		int s1 = stride * 3;



		int cnt = count;

	//	for (int i = 0; i < cnt; i++)
	//		FillPhong(i, p, iptr, dptr, stride, rconfig.renderWidth, rconfig.renderHeight, ca, co, si, nearZ, farZ, tanVert, tanHorz, rw, rh, fw, fh, oh, ow, s1, FC, pc);		


		parallel_for(0, cnt, [&](int index){
			FillPhong(index, p, iptr, dptr, stride, rconfig.renderWidth, rconfig.renderHeight, ca, co, si, nearZ, farZ, tanVert, tanHorz, rw, rh, fw, fh, oh, ow, s1, FC, pc);
		});



	}

	DLL void DepthFill(float* dptr, float* p, long count, long stride, vec3 co, vec3 si, vec3 ca, RenderSettings rconfig, long FC)
	{
		float radsFOV = rconfig.degFOV * M_PI / 180.0f;

		float nearZ = rconfig.nearZ, farZ = rconfig.farZ;

		float fovCoefficient = (float)tan((M_PI / 2.0f) - (radsFOV / 2.0f));
		float hFovCoefficient = ((float)rconfig.renderWidth / (float)rconfig.renderHeight) * (float)tan((M_PI / 2.0f) - (radsFOV / 2.0f));

		float tanVert = (float)tan(radsFOV / 2.0f) * (1.0f - 0.0f);
		float tanHorz = (float)tan(radsFOV / 2.0f) * ((float)rconfig.renderHeight / (float)rconfig.renderWidth) * (1.0f - 0.0f);

		float rw = (rconfig.renderWidth - 1.0f) / 2.0f, rh = (rconfig.renderHeight - 1.0f) / 2.0f;

		float fw = rw * fovCoefficient;
		float fh = rh * hFovCoefficient;

		float oh = 0;
		float ow = 0;

		int s1 = stride * 3;

		

		int cnt = count;

			
	//	for (int i = 0; i < cnt; i++)
	//		FillDepth(i, p, dptr, stride, rconfig.renderWidth, rconfig.renderHeight, ca, co, si, nearZ, farZ, tanVert, tanHorz, rw, rh, fw, fh, oh, ow, s1, FC);


		parallel_for(0, cnt, [&](int index){
			FillDepth(index, p, dptr, stride, rconfig.renderWidth, rconfig.renderHeight, ca, co, si, nearZ, farZ, tanVert, tanHorz, rw, rh, fw, fh, oh, ow, s1, FC);
		});



	}

}
