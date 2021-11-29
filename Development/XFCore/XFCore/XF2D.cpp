#include "stdafx.h"
#include <malloc.h>
#include <stdlib.h>
#include <omp.h>
#include "XFCore.h"

//instruct->
//int: Byte Position
//int: Size	
//int: Pointer	


inline void fmemcpy(unsigned char* dest, unsigned char* src, int count)
{
	for (int i = 0; i < count; ++i)
		dest[i] = src[i];
}


extern "C"
{
	DLL void ClearColor(long* TargetBuffer, long Width, long Height, long Color)
	{
		int* ptr = (int*)TargetBuffer;
		int col = Color;

#pragma omp parallel for
		for (int h = 0; h < Height; ++h)
		{
			int* iptr = ptr + Width * h;
			for (int w = 0; w < Width; ++w)
			{
				iptr[w] = col;
			}
		}
	}

	DLL void Pass(void* Shader, long Width, long Height, long* sMem, long sSize, long* iInstr, long iSize, long xyPOS)
	{
		void(*FS)(unsigned char*) = (void(*)(unsigned char*))Shader;
	
		unsigned char* Smemc = (unsigned char*)sMem;

#pragma omp parallel for
		for (int h = 0; h < Height; ++h)
		{
			unsigned char* bptr = (unsigned char*)alloca(sSize);
			fmemcpy(bptr, Smemc, sSize);
			unsigned char** bbptr = (unsigned char**)bptr;

			for (int i = 0; i < iSize; ++i)
			{
				//potentially undefined behavior if sizeof pointer changes
				*((int*)(bptr + iInstr[i * 3])) = (int)((unsigned char*)iInstr[i * 3 + 2] + iInstr[i * 3 + 1] * h * Width);
			}

			for (int w = 0; w < Width; ++w)
			{
				if (xyPOS != -1){
					int* trg = (int*)(bptr + xyPOS);
					trg[0] = w;
					trg[1] = h;
				}

				FS(bptr);

				for (int i = 0; i < iSize; ++i)
				{
					*((unsigned char**)(bptr + iInstr[i * 3])) += iInstr[i * 3 + 1];
					//*((unsigned char**)(bptr + iInstr[i * 3])) += 4;

				}
			}
		}
	}

	DLL void Copy32bpp(int* dest, int* src, int w1, int w2, int h1, int h2, int wSrc, int wDest, int wOffset, int hOffset, int hFlip)
	{
#pragma omp parallel for
		for (int h = h1; h < h2; ++h)
		{
			for (int w = w1; w < w2; ++w)
			{
				dest[(h + hOffset) * wDest + w + wOffset] = src[(hFlip - h) * wSrc + w];
			}
		}
	}

	DLL void Copy32bpp_Alpha(int* dest, int* src, int w1, int w2, int h1, int h2, int wSrc, int wDest, int wOffset, int hOffset, int hFlip)
	{
		const float toFloat = 1.0f / 255.0f;

#pragma omp parallel for
		for (int h = h1; h < h2; ++h)
		{
			for (int w = w1; w < w2; ++w)
			{
				unsigned char* S = (unsigned char*)(src + ((hFlip - h) * wSrc + w));
				unsigned char* D = (unsigned char*)(dest + ((h + hOffset) * wDest + w + wOffset));

				if (S[3] != 255)
				{
					float mult = S[3] * toFloat;

					D[0] = S[0] * mult + D[0] * (1.0f - mult);
					D[1] = S[1] * mult + D[1] * (1.0f - mult);
					D[2] = S[2] * mult + D[2] * (1.0f - mult);				
				}
				else
				{
					dest[(h + hOffset) * wDest + w + wOffset] = src[(hFlip - h) * wSrc + w];
				}			
			}
		}
	}

}
