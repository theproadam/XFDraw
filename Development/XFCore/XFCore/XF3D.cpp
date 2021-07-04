#include "stdafx.h"
#include <malloc.h>
#include <stdlib.h>
#include <omp.h>
#include "XFCore.h"
#include <math.h>
#define M_PI 3.14159265358979323846f

void FIP(float* TA, int INDEX, float* VD, int A, int B, float LinePos)
{
	float X;
	float Y;

	A *= 3;
	B *= 3;

	if (VD[A + 2] - VD[B + 2] != 0.0f)
	{
		float slopeY = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
		float bY = -slopeY * VD[A + 2] + VD[A + 1];
		Y = slopeY * LinePos + bY;

		float slopeX = (VD[A + 0] - VD[B + 0]) / (VD[A + 2] - VD[B + 2]);
		float bX = -slopeX * VD[A + 2] + VD[A + 0];
		X = slopeX * LinePos + bX;
	}

	TA[INDEX] = X;
	TA[INDEX + 1] = Y;
	TA[INDEX + 2] = LinePos;
}

void SIP(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, float oW)
{
	float X;
	float Y;
	float Z;

	A *= 3;
	B *= 3;

	float s1 = VD[A + 0] - VD[B + 0];
	float s2 = VD[A + 2] - VD[B + 2];
	s1 *= s1;
	s2 *= s2;
	//TODO clean this code up!

	if (s2 > s1)
	{
		float slope = (VD[A] - VD[B]) / (VD[A + 2] - VD[B + 2]);
		float b = -slope * VD[A + 2] + VD[A];

		float V = (b - oW) / (TanSlope - slope);

		X = V * slope + b;
		Z = V;
	}
	else
	{
		float slope = (VD[A + 2] - VD[B + 2]) / (VD[A] - VD[B]);
		float b = -slope * VD[A] + VD[A + 2];

		Z = (slope * oW + b) / (1.0f - slope * TanSlope);
		X = TanSlope * Z + oW;
	}


	//FLOATING POINT PRECESION ISSUES WITH X - Y != 0 BUT RATHER A VERY VERY SMALL NUMBER
	//SOLUTION INTERPOLATE BASED OF LARGEST NUMBER
	if (s1 > s2)
	{
		float slope = (VD[A + 1] - VD[B + 1]) / (VD[A] - VD[B]);
		float b = -slope * VD[A] + VD[A + 1];

		Y = slope * X + b;
	}
	else
	{
		float slope = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
		float b = -slope * VD[A + 2] + VD[A + 1];

		Y = slope * Z + b;
	}

	TA[INDEX] = X;
	TA[INDEX + 1] = Y;
	TA[INDEX + 2] = Z;
}

void SIPH(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, float oH)
{
	float X;
	float Y;
	float Z;

	A *= 3;
	B *= 3;

	float s1 = VD[A + 1] - VD[B + 1];
	float s2 = VD[A + 2] - VD[B + 2];
	s1 *= s1;
	s2 *= s2;

	if (s2 > s1)
	{
		float slope = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
		float b = -slope * VD[A + 2] + VD[A + 1];

		float V = (b - oH) / (TanSlope - slope);

		Y = V * slope + b;
		Z = V;
	}
	else
	{
		float slope = (VD[A + 2] - VD[B + 2]) / (VD[A + 1] - VD[B + 1]);
		float b = -slope * VD[A + 1] + VD[A + 2];

		Z = (slope * oH + b) / (1.0f - slope * TanSlope);
		Y = TanSlope * Z + oH;
	}

	//Floating point precision errors require this code:
	if (s1 > s2)
	{
		float slope = (VD[A] - VD[B]) / (VD[A + 1] - VD[B + 1]);
		float b = -slope * VD[A + 1] + VD[A];

		X = slope * Y + b;
	}
	else
	{
		float slope = (VD[A] - VD[B]) / (VD[A + 2] - VD[B + 2]);
		float b = -slope * VD[A + 2] + VD[A];

		X = slope * Z + b;
	}

	TA[INDEX] = X;
	TA[INDEX + 1] = Y;
	TA[INDEX + 2] = Z;
}

void LIP(float* XR, int I, float* V_DATA, int A, int B, int LinePos)
{
	float X = 0;
	float Z = 0;

	A *= 3;
	B *= 3;

	if (V_DATA[A + 1] == LinePos)
	{
		XR[I * 2] = V_DATA[A];
		XR[I * 2 + 1] = V_DATA[A + 2];
		return;
	}

	if (V_DATA[B + 1] == LinePos)
	{
		XR[I * 2] = V_DATA[B];
		XR[I * 2 + 1] = V_DATA[B + 2];
		return;
	}

	if (V_DATA[A + 1] - V_DATA[B + 1] != 0)
	{
		float slope = (V_DATA[A] - V_DATA[B]) / (V_DATA[A + 1] - V_DATA[B + 1]);
		float b = -slope * V_DATA[A + 1] + V_DATA[A];
		X = slope * LinePos + b;

		float slopeZ = (V_DATA[A + 2] - V_DATA[B + 2]) / (V_DATA[A + 1] - V_DATA[B + 1]);
		float bZ = -slopeZ * V_DATA[A + 1] + V_DATA[A + 2];
		Z = slopeZ * LinePos + bZ;
	}
	else
	{
		//throw new Exception("il fix this later");
		//this shoud not occur!
	}

	XR[I * 2] = X;
	XR[I * 2 + 1] = Z;
}

bool ScanLine(int Line, float* TRIS_DATA, int TRIS_SIZE, float* Intersects)
{
	int IC = 0;
	for (int i = 0; i < TRIS_SIZE; i++)
	{
		if (TRIS_DATA[i * 3 + 1] <= Line)
		{
			if (i == 0 && TRIS_DATA[(TRIS_SIZE - 1) * 3 + 1] >= Line)
			{
				LIP(Intersects, IC, TRIS_DATA, TRIS_SIZE - 1, i, Line);
				IC++;

				if (IC >= 2) break;
			}
			else if (i > 0 && TRIS_DATA[(i - 1) * 3 + 1] >= Line)
			{
				LIP(Intersects, IC, TRIS_DATA, i - 1, i, Line);
				IC++;

				if (IC >= 2) break;
			}
		}
		else if (TRIS_DATA[i * 3 + 1] > Line)
		{
			if (i == 0 && TRIS_DATA[(TRIS_SIZE - 1) * 3 + 1] <= Line)
			{
				LIP(Intersects, IC, TRIS_DATA, TRIS_SIZE - 1, i, Line);
				IC++;

				if (IC >= 2) break;
			}
			else if (i > 0 && TRIS_DATA[(i - 1) * 3 + 1] <= Line)
			{
				LIP(Intersects, IC, TRIS_DATA, i - 1, i, Line);
				IC++;

				if (IC >= 2) break;
			}
		}
	}


	if (IC == 2)
	{
		return true;
	}
	else return false;

}


