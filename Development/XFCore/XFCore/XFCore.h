#include <math.h>
#define DLL __declspec(dllexport)

extern bool CountTriangles;
extern bool CountPixels;
extern bool ForceUseOpenMP;

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

inline int FastInt(unsigned char R, unsigned char G, unsigned char B)
{
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