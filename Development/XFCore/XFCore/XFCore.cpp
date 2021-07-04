#include "stdafx.h"
#include "XFCore.h"

bool CountTriangles = false;
bool CountPixels = false;
bool ForceUseOpenMP = false;

extern "C"
{
	DLL int SizeCheck()
	{
		return sizeof(int*);
	}

	DLL void SetGLInfoMode(bool TC, bool PC)
	{
		CountPixels = PC;
		CountTriangles = TC;
	}

	DLL void SetParallelizationMode(bool useOpenMP, long LongThreadCount)
	{
		ForceUseOpenMP = useOpenMP;
	}
}

