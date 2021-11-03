#include <math.h>
#define DLL __declspec(dllexport)

extern bool CountTriangles;
extern bool CountPixels;
extern bool ForceUseOpenMP;

#define byte unsigned char
struct vec2
{
	float x;
	float y;

	vec2(float X, float Y)
	{
		x = X;
		y = Y;
	}

	vec2()
	{
		x = 0;
		y = 0;
	}

	vec2 operator+(const vec2& a) const
	{
		return vec2(a.x + x, a.y + y);
	}

	vec2 operator-(const vec2& a) const
	{
		return vec2(x - a.x, y - a.y);
	}

	vec2 operator*(const float& a) const
	{
		return vec2(a * x, a * y);
	}

	vec2 operator-() const
	{
		return vec2(-x, -y);
	}

	vec2 operator*(const vec2& a) const
	{
		return vec2(a.x * x, a.y * y);
	}
};

struct vec3
{
	float x;
	float y;
	float z;

	vec3()
	{
		
	}

	vec3(float X, float Y, float Z)
	{
		x = X;
		y = Y;
		z = Z;
	}

	vec3& operator =(const vec3& a)
	{
		x = a.x;
		y = a.y;
		z = a.z;
		return *this;
	}

	vec3 operator+(const vec3& a) const
	{
		return vec3(a.x + x, a.y + y, a.z + z);
	}

	vec3 operator-(const vec3& a) const
	{
		return vec3(a.x - x, a.y - y, a.z - z);
	}

	vec3 operator*(const float& a) const
	{
		return vec3(a * x, a * y, a * z);
	}

	vec3 operator-() const
	{
		return vec3(-x, -y, -z);
	}

	vec3 operator*(const vec3& a) const
	{
		return vec3(a.x * x, a.y * y, a.z * z);
	}

	void Clamp01()
	{
		if (x < 0) x = 0;
		else if (x > 1) x = 1;

		if (y < 0) y = 0;
		else if (y > 1) y = 1;

		if (z < 0) z = 0;
		else if (z > 1) z = 1;
	}
};

struct mat3
{
	float X0Y0;
	float X1Y0;
	float X2Y0;

	float X0Y1;
	float X1Y1;
	float X2Y1;

	float X0Y2;
	float X1Y2;
	float X2Y2;

	vec3 operator*(const vec3& B) const
	{
		vec3 result;
		result.x = X0Y0 * B.x + X1Y0 * B.y + X2Y0 * B.z;
		result.y = X0Y1 * B.x + X1Y1 * B.y + X2Y1 * B.z;
		result.z = X0Y2 * B.x + X1Y2 * B.y + X2Y2 * B.z;

		return result;
	}

	mat3 operator*(const mat3& B) const
	{
		mat3 result = mat3();

		result.X0Y0 = X0Y0 * B.X0Y0 + X1Y0 * B.X0Y1 + X2Y0 * B.X0Y2;
		result.X1Y0 = X0Y0 * B.X1Y0 + X1Y0 * B.X1Y1 + X2Y0 * B.X1Y2;
		result.X2Y0 = X0Y0 * B.X2Y0 + X1Y0 * B.X2Y1 + X2Y0 * B.X2Y2;

		result.X0Y1 = X0Y1 * B.X0Y0 + X1Y1 * B.X0Y1 + X2Y1 * B.X0Y2;
		result.X1Y1 = X0Y1 * B.X1Y0 + X1Y1 * B.X1Y1 + X2Y1 * B.X1Y2;
		result.X2Y1 = X0Y1 * B.X2Y0 + X1Y1 * B.X2Y1 + X2Y1 * B.X2Y2;

		result.X0Y2 = X0Y2 * B.X0Y0 + X1Y2 * B.X0Y1 + X2Y2 * B.X0Y2;
		result.X1Y2 = X0Y2 * B.X1Y0 + X1Y2 * B.X1Y1 + X2Y2 * B.X1Y2;
		result.X2Y2 = X0Y2 * B.X2Y0 + X1Y2 * B.X2Y1 + X2Y2 * B.X2Y2;

		return result;
	}

};

inline vec3 normalize(vec3 value)
{
	float num = 1.0f / sqrtf(value.x * value.x + value.y * value.y + value.z * value.z);

	if (num > 1E-05f)
	{
		return value * num;
	}
	else return vec3(0, 0, 0);
}

inline float dot(vec3 lhs, vec3 rhs)
{
	return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
}

inline vec3 reflect(vec3 inDirection, vec3 inNormal)
{
	return inNormal * -2.0f * dot(inNormal, inDirection) + inDirection;
}

inline vec3 ToCameraSpace(vec3 input, vec3 co, vec3 si)
{
	float fiX = input.x * co.z - input.z * si.z;
	float fiZ = input.z * co.z + input.x * si.z;
	float ndY = input.y * co.y + fiZ * si.y;

	return vec3(fiX * co.x - ndY * si.x, ndY * co.x + fiX * si.x, fiZ * co.y - input.y * si.y);
}

inline void ToCameraSpace(float* input, float* output, vec3 pos, vec3 co, vec3 si)
{
	float X = input[0] - pos.x;
	float Y = input[1] - pos.y;
	float Z = input[2] - pos.z;

	float fiX = X * co.z - Z * si.z;
	float fiZ = Z * co.z + X * si.z;
	float ndY = Y * co.y + fiZ * si.y;

	output[0] = fiX * co.x - ndY * si.x;
	output[1] = ndY * co.x + fiX * si.x;
	output[2] = fiZ * co.y - Y * si.y;
}

inline void ToXY(vec3 XYZ, float srw, float srh, float sfw, float sfh, float* X, float* Y)
{
	float Z = 1.0f / XYZ.z;
	*X = srw + XYZ.x * Z * sfw;
	*Y = srh + XYZ.y * Z * sfh;
}


struct GLData
{
	float nearZ;
	float farZ;

	float tanVert;
	float tanHorz;

	float ow;
	float oh;

	float rw;
	float rh;

	float fw;
	float fh;

	float ox;
	float oy;

	float iox;
	float ioy;

	float oValue;

	int renderWidth;
	int renderHeight;

	float matrixlerpv;
};

inline unsigned char clamp255(float input)
{
	if (input >= 255.0f)
		return 255;
	else return (unsigned char)input;
}

struct RenderSettings
{
	float farZ;
	float nearZ;
	int renderWidth;
	int renderHeight;
	float degFOV;
};

inline int DrawLine(int* iptr, int rw, int diValue, float fromX, float fromY, float toX, float toY)
{
	if (fromX == toX && fromY == toY)
		return 0;

	int ret = 0;
	// Buffer OverFlow Protection will still be needed regardless how polished the code is...
	//moved to prepos. This means that during a posX/Y -> infinity the program will crash rather than freeze (is there really a difference)

	float aa = (fromX - toX);
	float ba = (fromY - toY);

	if (aa * aa > ba * ba)
	{
		float slope = (fromY - toY) / (fromX - toX);
		float b = -slope * fromX + fromY;

		if (fromX < toX){
			ret = toX - fromX;
			for (int i = (int)fromX; i <= toX; ++i)
			{
				int tY = (int)(i * slope + b);
				*(iptr + rw * tY + i) = diValue;
			}
		}
		else{
			ret = fromX - toX;
			for (int i = (int)toX; i <= fromX; ++i)
			{
				int tY = (int)(i * slope + b);
				*(iptr + rw * tY + i) = diValue;
			}
		}
	}
	else
	{
		float slope = (fromX - toX) / (fromY - toY);
		float b = -slope * fromY + fromX;

		if (fromY < toY){
			ret = toY - fromY;
			for (int i = (int)fromY; i <= toY; ++i)
			{
				int tY = (int)(i * slope + b);
				*(iptr + rw * i + tY) = diValue;
			}
		}
		else{
			ret = fromY - toY;
			for (int i = (int)toY; i <= fromY; ++i)
			{
				int tY = (int)(i * slope + b);
				*(iptr + rw * i + tY) = diValue;
			}
		}

	}

	return ret;
}

inline void DrawLineFast(int* iptr, int rw, int diValue, float fromX, float fromY, float toX, float toY)
{
	if (fromX == toX && fromY == toY)
		return;

	float aa = (fromX - toX);
	float ba = (fromY - toY);

	if (aa * aa > ba * ba)
	{
		float slope = (fromY - toY) / (fromX - toX);
		
		if (fromX < toX){
			float t = fromY;
			for (int i = (int)fromX; i <= toX; ++i, t += slope)
			{
				*(iptr + rw * (int) t + i) = diValue;
			}
		}
		else{
			float t = toY;
			for (int i = (int)toX; i <= fromX; ++i, t += slope)
			{
				*(iptr + rw * (int)t + i) = diValue;
			}
		}
	}
	else
	{
		float slope = (fromX - toX) / (fromY - toY);

		if (fromY < toY){
			float t = fromX;
			for (int i = (int)fromY; i <= toY; ++i, t += slope)
			{
				*(iptr + rw * i + (int)t) = diValue;
			}
		}
		else{
			float t = toX;
			for (int i = (int)toY; i <= fromY; ++i, t += slope)
			{
				*(iptr + rw * i + (int)t) = diValue;
			}
		}

	}
}

inline void DrawLineDATA_OLD(float* FromDATA, float* ToDATA, float* dptr, unsigned char* bptr,
	float* ScratchSpace, int Index, int Stride, float farZ, int renderHeight, int renderWidth, int wsD, int sD)
{
	if (FromDATA[0] == ToDATA[0] && FromDATA[1] == ToDATA[1])
		return;

	float oValue = 0; 
	float zoffset = 0;

	float* ATB = ScratchSpace + (Stride - 3) * 3;
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
			ScratchSpace[(s - 3) * 2] = (FromDATA[s] - ToDATA[s]) / (1.0f / FromDATA[2] - 1.0f / ToDATA[3]);
			ScratchSpace[(s - 3) * 2 + 1] = -ScratchSpace[(s - 3) * 2] / FromDATA[2] + ToDATA[s];
		}

		if (FromDATA[0] > ToDATA[0])
		{
			float* temp = ToDATA;
			ToDATA = FromDATA;
			FromDATA = temp;
		}

		for (int i = (int)FromDATA[0]; i <= ToDATA[0]; i++)
		{
			int tY = (int)(i * slope + b);
			zz = (1.0f / (slopeZ * (float)i + bZ) - oValue);

			float s = farZ - zz;
			if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

			if (dptr[renderWidth * tY + i] > s - zoffset) continue;
			dptr[renderWidth * tY + i] = s;

			for (int z = 0; z < Stride - 3; z++)
				ScratchSpace[(Stride - 3) * 2 + z] = (ScratchSpace[z * 2] / (slopeZ * (float)i + bZ) + ScratchSpace[z * 2 + 1]);

			//FS(bptr + (tY * wsD + (i * sD)), ScratchSpace + (Stride - 3) * 2, Index);
			unsigned char* ptr = bptr + (tY * wsD + (i * sD));
			float* vdata = ScratchSpace + (Stride - 3) * 2;

			{
				ptr[0] = vdata[0] * 127.5f + 127.5f;
				ptr[1] = vdata[1] * 127.5f + 127.5f;
				ptr[2] = vdata[2] * 127.5f + 127.5f;


			}
		}

	}
	else
	{
		float slope = (FromDATA[0] - ToDATA[0]) / (FromDATA[1] - ToDATA[1]);
		float b = -slope * FromDATA[1] + FromDATA[0];

		float slopeZ = (FromDATA[2] - ToDATA[2]) / (FromDATA[1] - ToDATA[1]);
		float bZ = -slopeZ * FromDATA[1] + FromDATA[2];

		for (int s = 3; s < Stride; s++)
		{
			ScratchSpace[(s - 3) * 2] = (FromDATA[s] - ToDATA[s]) / (1.0f / FromDATA[2] - 1.0f / ToDATA[3]);
			ScratchSpace[(s - 3) * 2 + 1] = -ScratchSpace[(s - 3) * 2] / FromDATA[2] + ToDATA[s];
		}

		if (FromDATA[1] > ToDATA[1])
		{
			float* temp = ToDATA;
			ToDATA = FromDATA;
			FromDATA = temp;
		}

		for (int i = (int)FromDATA[1]; i <= ToDATA[1]; i++)
		{
			int tY = (int)(i * slope + b);

			zz = (1.0f / (slopeZ * (float)i + bZ) - oValue);


			float s = farZ - zz;
			if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;

			if (dptr[renderWidth * i + tY] > s - zoffset) continue;
			dptr[renderWidth * i + tY] = s;


			for (int z = 0; z < Stride - 3; z++)
				ScratchSpace[(Stride - 3) * 2 + z] = (ScratchSpace[z * 2] / (slopeZ * (float)i + bZ) + ScratchSpace[z * 2 + 1]);

			//FS(bptr + (i * wsD + (tY * sD)), ScratchSpace + (Stride - 3) * 2, Index);
			unsigned char* ptr = bptr + (tY * wsD + (i * sD));
			float* vdata = ScratchSpace + (Stride - 3) * 2;

			{
				ptr[0] = vdata[0] * 127.5f + 127.5f;
				ptr[1] = vdata[1] * 127.5f + 127.5f;
				ptr[2] = vdata[2] * 127.5f + 127.5f;


			}
		}
	}
}

inline void DrawLineNoDATA(float* FromDATA, float* ToDATA, float* dptr, int* iptr, int color, float zoffset, int Stride, int VW, int VH, float farZ, bool perspMat, float oValue, int offsetmod)
{
	if (FromDATA[0] == ToDATA[0] && FromDATA[1] == ToDATA[1])
		return;

	float aa = (FromDATA[0] - ToDATA[0]);
	float ba = (FromDATA[1] - ToDATA[1]);

	if (aa * aa > ba * ba)
	{
		float slope = (FromDATA[1] - ToDATA[1]) / (FromDATA[0] - ToDATA[0]);
		float b = -slope * FromDATA[0] + FromDATA[1];

		float slopeZ = (FromDATA[2] - ToDATA[2]) / (FromDATA[0] - ToDATA[0]);
		float bZ = -slopeZ * FromDATA[0] + FromDATA[2];

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

			iptr[mem_addr] = color;
		}
	}
	else
	{
		float slope = (FromDATA[0] - ToDATA[0]) / (FromDATA[1] - ToDATA[1]);
		float b = -slope * FromDATA[1] + FromDATA[0];

		float slopeZ = (FromDATA[2] - ToDATA[2]) / (FromDATA[1] - ToDATA[1]);
		float bZ = -slopeZ * FromDATA[1] + FromDATA[2];

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

			iptr[mem_addr] = color;
		}
	}
}

inline void DrawLineNoDATA_AA(float* FromDATA, float* ToDATA, float* dptr, int* iptr, byte dR, byte dG, byte dB, int col, bool AALWR, bool AAUPR, float zoffset, int Stride, int VW, int VH, float farZ, bool perspMat, float oValue, int offsetmod)
{
	if (FromDATA[0] == ToDATA[0] && FromDATA[1] == ToDATA[1])
		return;
	
	float aa = (FromDATA[0] - ToDATA[0]);
	float ba = (FromDATA[1] - ToDATA[1]);

	if (aa * aa > ba * ba)
	{
		float slope = (FromDATA[1] - ToDATA[1]) / (FromDATA[0] - ToDATA[0]);
		float b = -slope * FromDATA[0] + FromDATA[1];

		float slopeZ = (FromDATA[2] - ToDATA[2]) / (FromDATA[0] - ToDATA[0]);
		float bZ = -slopeZ * FromDATA[0] + FromDATA[2];

		if (FromDATA[0] > ToDATA[0])
		{
			float* temp = ToDATA;
			ToDATA = FromDATA;
			FromDATA = temp;
		}

		for (int i = (int)FromDATA[0]; i <= ToDATA[0]; i++)
		{
			float trueY = ((float)i * slope + b) + offsetmod;
			int tY = (int)trueY;
			float depth = perspMat ? (1.0f / (slopeZ * (float)i + bZ) - oValue) : (slopeZ * (float)i + bZ);

			float s = farZ - depth;
			if (i < 0 || tY < 0 || tY >= VH || i >= VW) continue;

			int mem_addr = VW * tY + i;

			if (dptr[mem_addr] > s - zoffset) continue;
			dptr[mem_addr] = s;

			//COLOR WRITE
			float MB = (float)tY + 1.0f - trueY;
			float MT = trueY - (float)tY;

			unsigned char* lptr = (unsigned char*)(iptr + mem_addr);

			if (AALWR)
			{
				*(lptr + 0) = clamp255((*(lptr + 0) * (1.0f - MB)) + MB * dB);
				*(lptr + 1) = clamp255((*(lptr + 1) * (1.0f - MB)) + MB * dG);
				*(lptr + 2) = clamp255((*(lptr + 2) * (1.0f - MB)) + MB * dR);
			}
			else
			{
				iptr[mem_addr] = col;
			}

			if (AAUPR)
			{
				if (tY + 1 >= VH) continue;
				lptr = (unsigned char*)(iptr + mem_addr + VW);

				*(lptr + 0) = clamp255(*(lptr + 0) * (1.0f - MT) + MT * dB);
				*(lptr + 1) = clamp255(*(lptr + 1) * (1.0f - MT) + MT * dG);
				*(lptr + 2) = clamp255(*(lptr + 2) * (1.0f - MT) + MT * dR);
			}
						
		//	iptr[mem_addr] = color;
		}
	}
	else
	{
		float slope = (FromDATA[0] - ToDATA[0]) / (FromDATA[1] - ToDATA[1]);
		float b = -slope * FromDATA[1] + FromDATA[0];

		float slopeZ = (FromDATA[2] - ToDATA[2]) / (FromDATA[1] - ToDATA[1]);
		float bZ = -slopeZ * FromDATA[1] + FromDATA[2];

		if (FromDATA[1] > ToDATA[1])
		{
			float* temp = ToDATA;
			ToDATA = FromDATA;
			FromDATA = temp;
		}

		for (int i = (int)FromDATA[1]; i <= ToDATA[1]; i++)
		{
			float trueY = ((float)i * slope + b) + offsetmod;
			int tY = (int)trueY;
			float depth = perspMat ? (1.0f / (slopeZ * (float)i + bZ) - oValue) : (slopeZ * (float)i + bZ);

			float s = farZ - depth;
			if (i < 0 || tY < 0 || tY >= VW || i >= VH) continue;

			int mem_addr = VW * i + tY;

			if (dptr[mem_addr] > s - zoffset) continue;
			dptr[mem_addr] = s;


			//COLOR WRITE
			float MB = (float)tY + 1.0f - trueY;
			float MT = trueY - (float)tY;

			byte* lptr = (unsigned char*)(iptr + mem_addr);

			if (AALWR)
			{
				*(lptr + 0) = clamp255(*(lptr + 0) * (1.0f - MB) + MB * dB);
				*(lptr + 1) = clamp255(*(lptr + 1) * (1.0f - MB) + MB * dG);
				*(lptr + 2) = clamp255(*(lptr + 2) * (1.0f - MB) + MB * dR);
			}
			else
			{
				iptr[mem_addr] = col;
			}

			if (AAUPR)
			{
				if (tY + 1 >= VW) continue;
				lptr += 4;

				*(lptr + 0) = clamp255(*(lptr + 0) * (1.0f - MT) + MT * dB);
				*(lptr + 1) = clamp255(*(lptr + 1) * (1.0f - MT) + MT * dG);
				*(lptr + 2) = clamp255(*(lptr + 2) * (1.0f - MT) + MT * dR);
			}
	

			//iptr[mem_addr] = color;
		}
	}
}



struct byte4
{
	unsigned char B;
	unsigned char G;
	unsigned char R;
	unsigned char A;

	byte4(unsigned char r, unsigned char g, unsigned char b)
	{
		A = 255;
		R = r;
		G = g;
		B = b;
	}

	byte4(unsigned char a, unsigned char r, unsigned char g, unsigned char b)
	{
		A = a;
		R = r;
		G = g;
		B = b;
	}

	byte4()
	{
		B = 0;
		G = 0;
		R = 0;
		A = 0;
	}

	vec3 xyz()
	{
		return vec3(R, G, B);
	}
};


void FIP(float* TA, int INDEX, float* VD, int A, int B, float LinePos);
void SIP(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, float oW);
void SIPH(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, float oH);

void LIP(float* XR, int I, float* V_DATA, int A, int B, int LinePos);
bool ScanLine(int Line, float* TRIS_DATA, int TRIS_SIZE, float* Intersects);


void FIPA(float* TA, int INDEX, float* VD, int A, int B, float LinePos, int Stride);
void SIPA(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, float oW, int Stride);
void SIPHA(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, float oH, int Stride);

void LIPA(float* XR, int I, float* V_DATA, int A, int B, int LinePos, int Stride);

bool ScanLinePLUS(int Line, float* TRIS_DATA, int TRIS_SIZE, float* Intersects, int Stride);
bool ScanLinePLUS_(int Line, float* TRIS_DATA, int TRIS_SIZE, float* Intersects, int Stride);


inline int FastInt(unsigned char R, unsigned char G, unsigned char B)
{
	//return max(B, 255) + 256 * max(G, 255) + max(R, 255) * 65536;
	return B + 256 * G + R * 65536;

}



inline float BACKFACECULL3(float* VERTEX_DATA)
{
	return ((VERTEX_DATA[3]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[7]) - (VERTEX_DATA[1])) - ((VERTEX_DATA[6]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[4]) - (VERTEX_DATA[1]));
}

inline float BACKFACECULLS(float* VERTEX_DATA, int Stride)
{
	return ((VERTEX_DATA[Stride]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[Stride * 2 + 1]) - (VERTEX_DATA[1])) - ((VERTEX_DATA[Stride * 2]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[Stride + 1]) - (VERTEX_DATA[1]));
}