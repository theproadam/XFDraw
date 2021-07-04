#include "stdafx.h"
#include "xfcore.h"
#include <stdlib.h>
int TEXTURE_WRAP_MODE = 0;
byte4 TEXTURE_CLAMP_BORDER_COLOR = byte4(255, 0, 255);

byte4 textureNEAREST(sampler2D inputTexture, int2 coord)
{
	if (TEXTURE_WRAP_MODE == 0)
	{
		if (coord.X < 0) coord.X = 0;
		if (coord.Y < 0) coord.Y = 0;
		if (coord.X >= inputTexture.width) coord.X = inputTexture.width - 1;
		if (coord.Y >= inputTexture.height) coord.Y = inputTexture.height - 1;
	}
	else if (TEXTURE_WRAP_MODE == 1)
	{
		if (coord.X < 0) coord.X = coord.X % inputTexture.width + inputTexture.width;
		if (coord.Y < 0) coord.Y = coord.Y % inputTexture.height + inputTexture.height;

		if (coord.X >= inputTexture.width) coord.X = coord.X % inputTexture.width;
		if (coord.Y >= inputTexture.height) coord.Y = coord.Y % inputTexture.height;
	}
	else
	{
		if (coord.X < 0 || coord.Y < 0 || coord.X >= inputTexture.width || coord.Y >= inputTexture.height) return TEXTURE_CLAMP_BORDER_COLOR;
	}

	return *((byte4*)(inputTexture.TEXTURE_ADDR + inputTexture.width * coord.Y + coord.X));
}

byte4 textureBLINEAR(sampler2D inputTexture, vec2 coord)
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
		if (X0Y0.X >= inputTexture.width) X0Y0.X = inputTexture.width - 1;
		if (X0Y0.Y >= inputTexture.height) X0Y0.Y = inputTexture.height - 1;

		if (X1Y0.X < 0) X1Y0.X = 0;
		if (X1Y0.Y < 0) X1Y0.Y = 0;
		if (X1Y0.X >= inputTexture.width) X1Y0.X = inputTexture.width - 1;
		if (X1Y0.Y >= inputTexture.height) X1Y0.Y = inputTexture.height - 1;

		if (X0Y1.X < 0) X0Y1.X = 0;
		if (X0Y1.Y < 0) X0Y1.Y = 0;
		if (X0Y1.X >= inputTexture.width) X0Y1.X = inputTexture.width - 1;
		if (X0Y1.Y >= inputTexture.height) X0Y1.Y = inputTexture.height - 1;

		if (X1Y1.X < 0) X1Y1.X = 0;
		if (X1Y1.Y < 0) X1Y1.Y = 0;
		if (X1Y1.X >= inputTexture.width) X1Y1.X = inputTexture.width - 1;
		if (X1Y1.Y >= inputTexture.height) X1Y1.Y = inputTexture.height - 1;
	}
	else
	{
		if (X0Y0.X < 0) X0Y0.X = X0Y0.X % inputTexture.width + inputTexture.width;
		if (X0Y0.Y < 0) X0Y0.Y = X0Y0.Y % inputTexture.height + inputTexture.height;
		if (X0Y0.X >= inputTexture.width) X0Y0.X = X0Y0.X % inputTexture.width;
		if (X0Y0.Y >= inputTexture.height) X0Y0.Y = X0Y0.Y % inputTexture.height;

		if (X1Y0.X < 0) X1Y0.X = X1Y0.X % inputTexture.width + inputTexture.width;
		if (X1Y0.Y < 0) X1Y0.Y = X1Y0.Y % inputTexture.height + inputTexture.height;
		if (X1Y0.X >= inputTexture.width) X1Y0.X = X1Y0.X % inputTexture.width;
		if (X1Y0.Y >= inputTexture.height) X1Y0.Y = X1Y0.Y % inputTexture.height;

		if (X0Y1.X < 0) X0Y1.X = X0Y1.X % inputTexture.width + inputTexture.width;
		if (X0Y1.Y < 0) X0Y1.Y = X0Y1.Y % inputTexture.height + inputTexture.height;
		if (X0Y1.X >= inputTexture.width) X0Y1.X = X0Y1.X % inputTexture.width;
		if (X0Y1.Y >= inputTexture.height) X0Y1.Y = X0Y1.Y % inputTexture.height;

		if (X1Y1.X < 0) X1Y1.X = X1Y1.X % inputTexture.width + inputTexture.width;
		if (X1Y1.Y < 0) X1Y1.Y = X1Y1.Y % inputTexture.height + inputTexture.height;
		if (X1Y1.X >= inputTexture.width) X1Y1.X = X1Y1.X % inputTexture.width;
		if (X1Y1.Y >= inputTexture.height) X1Y1.Y = X1Y1.Y % inputTexture.height;
	}


	unsigned char* bptrL = (unsigned char*)(inputTexture.TEXTURE_ADDR + X0Y0.Y * inputTexture.width + X0Y0.X);
	unsigned char* bptrLN = (unsigned char*)(inputTexture.TEXTURE_ADDR + X1Y0.Y * inputTexture.width + X1Y0.X);

	unsigned char* bptrU = (unsigned char*)(inputTexture.TEXTURE_ADDR + X0Y1.Y * inputTexture.width + X0Y1.X);
	unsigned char* bptrUN = (unsigned char*)(inputTexture.TEXTURE_ADDR + X1Y1.Y * inputTexture.width + X1Y1.X);


	//unsigned char* bptrU = bptrU + inputTexture.width * 4;

	unsigned char B = bptrL[0] * (x0 * y0) + bptrLN[0] * (x1 * y0) + bptrU[0] * (x0 * y1) + bptrUN[0] * (x1 * y1);
	unsigned char G = bptrL[1] * (x0 * y0) + bptrLN[1] * (x1 * y0) + bptrU[1] * (x0 * y1) + bptrUN[1] * (x1 * y1);
	unsigned char R = bptrL[2] * (x0 * y0) + bptrLN[2] * (x1 * y0) + bptrU[2] * (x0 * y1) + bptrUN[2] * (x1 * y1);

	return byte4(R, G, B);

	//return *((byte4*)(inputTexture.TEXTURE_ADDR + inputTexture.width * coord.Y + coord.X));
}

inline void RotateVertex(vec3* XYZ_IN, vec3* XYZ_OUT, vec3 angleCOS, vec3 angleSIN, vec3 aroundPosition)
{
	float X = XYZ_IN->x - aroundPosition.x;
	float Y = XYZ_IN->y - aroundPosition.y;
	float Z = XYZ_IN->z - aroundPosition.z;

	float fiX = X * angleCOS.z - Z * angleSIN.z;
	float fiZ = Z * angleCOS.z + X * angleSIN.z;
	float ndY = Y * angleCOS.y + fiZ * angleSIN.y;

	float* output = (float*)XYZ_OUT;
	//Returns the newly rotated Vector
	output[0] = fiX * angleCOS.x - ndY * angleSIN.x;
	output[1] = ndY * angleCOS.x + fiX * angleSIN.x;	
	output[2] = fiZ * angleCOS.y - Y * angleSIN.y;
}

inline vec3 RotateVertex(vec3* XYZ_IN, vec3 angleCOS, vec3 angleSIN, vec3 aroundPosition)
{
	float X = XYZ_IN->x - aroundPosition.x;
	float Y = XYZ_IN->y - aroundPosition.y;
	float Z = XYZ_IN->z - aroundPosition.z;

	float fiX = X * angleCOS.z - Z * angleSIN.z;
	float fiZ = Z * angleCOS.z + X * angleSIN.z;
	float ndY = Y * angleCOS.y + fiZ * angleSIN.y;

	return vec3(fiX * angleCOS.x - ndY * angleSIN.x,
		ndY * angleCOS.x + fiX * angleSIN.x,
		fiZ * angleCOS.y - Y * angleSIN.y);
}


long CHECK_VALID_COMPILE()
{
	return sizeof(void*);
}

bool rmq;
int qmr;
long* taddr;

extern "C"
{
	__declspec(dllexport) long* InitializeDLLModule(int* size)
	{
		rmq = false;
		LoadModule();
		rmq = true;

		*size = qmr;

		//yeah i know this is a really bad way of doing this
		int msize = qmr * (4 + 4 + 4 + 4);
		taddr = (long*)malloc(msize);

		qmr = 0;

		LoadModule();

		return taddr;
	}

	__declspec(dllexport) void FreeMalloc()
	{
		free((void*)taddr);
	}
}
