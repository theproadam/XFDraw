#include "stdafx.h"
#include <omp.h>
#include "XFCore.h"
#include <ppl.h>
using namespace Concurrency;

extern "C" __declspec(dllexport) void Test()
{
	
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

}