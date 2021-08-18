#include <cstdint>
#include <math.h>

struct vec3
{
	float x;
	float y;
	float z;

	vec3()
	{
		x = 0;
		y = 0;
		z = 0;
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
		return vec3(x - a.x, y - a.y, z - a.z);
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

    vec2(vec3 in)
    {
        x = in.x;
        y = in.y;
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


struct vec4
{
	float x;
	float y;
	float z;
	float w;

	vec4()
	{
		x = 0;
		y = 0;
		z = 0;
		w = 0;
	}

	vec4(vec3 Vector3, float wValue)
	{
		x = Vector3.x;
		y = Vector3.y;
		z = Vector3.z;
		w = wValue;
	}

	vec3 tovec3()
	{
		return vec3(x, y, z);
	}
};

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

struct sampler2D
{
	int width;
	int height;
	byte4* TEXTURE_ADDR;
    int wrap_mode;
    byte4 wrap_mode_color;
    int filt_mode;
    int stride;
};

struct sampler1D
{
	int size;
	float* mem_addr;
	int stride;
};

struct samplerCube
{
	int width;
	int height;

	byte4* front;
	byte4* back;
	byte4* left;
	byte4* right;
	byte4* top;
	byte4* bottom;

	int wrap_mode;
	byte4 wrap_mode_color;
	int filt_mode;
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

    mat3()
	{
		
	}

    mat3(vec3 col1, vec3 col2, vec3 col3)
    {
        X0Y0 = col1.x;
        X0Y1 = col1.y;
        X0Y2 = col1.z;
        
        X1Y0 = col2.x;
        X1Y1 = col2.y;
        X1Y2 = col2.z;
        
        X2Y0 = col3.x;
        X2Y1 = col3.y;
        X2Y2 = col3.z;
       
    }

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

struct mat4
{
	float X0Y0;
	float X1Y0;
	float X2Y0;
	float X3Y0;

	float X0Y1;
	float X1Y1;
	float X2Y1;
	float X3Y1;

	float X0Y2;
	float X1Y2;
	float X2Y2;
	float X3Y2;

	float X0Y3;
	float X1Y3;
	float X2Y3;
	float X3Y3;

	vec4 operator*(const vec4& B) const
	{
		vec4 result;
		result.x = X0Y0 * B.x + X1Y0 * B.y + X2Y0 * B.z + X3Y0 * B.w;
		result.y = X0Y1 * B.x + X1Y1 * B.y + X2Y1 * B.z + X3Y1 * B.w;
		result.z = X0Y2 * B.x + X1Y2 * B.y + X2Y2 * B.z + X3Y2 * B.w;
		result.w = X0Y3 * B.x + X1Y3 * B.y + X2Y3 * B.z + X3Y3 * B.w;

		return result;
	}

	mat4 operator*(const mat4& B) const
	{
		mat4 result = mat4();

		result.X0Y0 = X0Y0 * B.X0Y0 + X1Y0 * B.X0Y1 + X2Y0 * B.X0Y2 + X3Y0 * B.X0Y3;
		result.X1Y0 = X0Y0 * B.X1Y0 + X1Y0 * B.X1Y1 + X2Y0 * B.X1Y2 + X3Y0 * B.X1Y3;
		result.X2Y0 = X0Y0 * B.X2Y0 + X1Y0 * B.X2Y1 + X2Y0 * B.X2Y2 + X3Y0 * B.X2Y3;
		result.X3Y0 = X0Y0 * B.X3Y0 + X1Y0 * B.X3Y1 + X2Y0 * B.X3Y2 + X3Y0 * B.X3Y3;

		result.X0Y1 = X0Y1 * B.X0Y0 + X1Y1 * B.X0Y1 + X2Y1 * B.X0Y2 + X3Y1 * B.X0Y3;
		result.X1Y1 = X0Y1 * B.X1Y0 + X1Y1 * B.X1Y1 + X2Y1 * B.X1Y2 + X3Y1 * B.X1Y3;
		result.X2Y1 = X0Y1 * B.X2Y0 + X1Y1 * B.X2Y1 + X2Y1 * B.X2Y2 + X3Y1 * B.X2Y3;
		result.X3Y1 = X0Y1 * B.X3Y0 + X1Y1 * B.X3Y1 + X2Y1 * B.X3Y2 + X3Y1 * B.X3Y3;

		result.X0Y2 = X0Y2 * B.X0Y0 + X1Y2 * B.X0Y1 + X2Y2 * B.X0Y2 + X3Y2 * B.X0Y3;
		result.X1Y2 = X0Y2 * B.X1Y0 + X1Y2 * B.X1Y1 + X2Y2 * B.X1Y2 + X3Y2 * B.X1Y3;
		result.X2Y2 = X0Y2 * B.X2Y0 + X1Y2 * B.X2Y1 + X2Y2 * B.X2Y2 + X3Y2 * B.X2Y3;
		result.X3Y2 = X0Y2 * B.X2Y0 + X1Y2 * B.X2Y1 + X2Y2 * B.X2Y2 + X3Y2 * B.X3Y3;

		result.X0Y3 = X0Y3 * B.X0Y0 + X1Y3 * B.X0Y1 + X2Y3 * B.X0Y2 + X3Y3 * B.X0Y3;
		result.X1Y3 = X0Y3 * B.X1Y0 + X1Y3 * B.X1Y1 + X2Y3 * B.X1Y2 + X3Y3 * B.X1Y3;
		result.X2Y3 = X0Y3 * B.X2Y0 + X1Y3 * B.X2Y1 + X2Y3 * B.X2Y2 + X3Y3 * B.X2Y3;
		result.X3Y3 = X0Y3 * B.X2Y0 + X1Y3 * B.X2Y1 + X2Y3 * B.X2Y2 + X3Y3 * B.X3Y3;

		return result;
	}

};

mat3 transpose(mat3 input)
{
	mat3 result;
	result.X0Y0 = input.X0Y0;
	result.X1Y0 = input.X0Y1;
	result.X2Y0 = input.X0Y2;
	
	result.X0Y1 = input.X1Y0;
	result.X1Y1 = input.X1Y1;
	result.X2Y1 = input.X1Y2;

	result.X0Y2 = input.X2Y0;
	result.X1Y2 = input.X2Y1;
	result.X2Y2 = input.X2Y2;
	return result;
}

void fcpy(char* dest, char* src, int count)
{
	for (int i = 0; i < count; ++i)
		dest[i] = src[i];
}

extern "C" __declspec(dllexport) int32_t CheckSize()
{
	return sizeof(void*);
}

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

inline vec3 refract(vec3 I, vec3 N, float eta)
{
	float k = 1.0f - eta * eta * (1.0f - dot(N, I) * dot(N, I));
	if (k < 0.0f)
	{
		return vec3();
	}
	else
	{
		return I * eta - N * (eta * dot(N, I) + sqrt(k));
	}
}

inline byte4 textureBILINEAR(sampler2D inputTexture, vec2 coord, bool sampleAlpha = false)
{
	if (inputTexture.stride != 4)
		return byte4(255, 0, 255);

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

	unsigned char* bptrL = (unsigned char*)(inputTexture.TEXTURE_ADDR + X0Y0.Y * inputTexture.width + X0Y0.X);
	unsigned char* bptrLN = (unsigned char*)(inputTexture.TEXTURE_ADDR + X1Y0.Y * inputTexture.width + X1Y0.X);

	unsigned char* bptrU = (unsigned char*)(inputTexture.TEXTURE_ADDR + X0Y1.Y * inputTexture.width + X0Y1.X);
	unsigned char* bptrUN = (unsigned char*)(inputTexture.TEXTURE_ADDR + X1Y1.Y * inputTexture.width + X1Y1.X);

	unsigned char B = bptrL[0] * (x0 * y0) + bptrLN[0] * (x1 * y0) + bptrU[0] * (x0 * y1) + bptrUN[0] * (x1 * y1);
	unsigned char G = bptrL[1] * (x0 * y0) + bptrLN[1] * (x1 * y0) + bptrU[1] * (x0 * y1) + bptrUN[1] * (x1 * y1);
	unsigned char R = bptrL[2] * (x0 * y0) + bptrLN[2] * (x1 * y0) + bptrU[2] * (x0 * y1) + bptrUN[2] * (x1 * y1);

	if (sampleAlpha)
	{
		unsigned char A = bptrL[3] * (x0 * y0) + bptrLN[3] * (x1 * y0) + bptrU[3] * (x0 * y1) + bptrUN[3] * (x1 * y1);
		return byte4(A, R, G, B);
	}

	return byte4(R, G, B);
}

inline byte4 textureNEAREST(sampler2D inputTexture, int2 coord)
{
	if (inputTexture.stride != 4)
		return byte4(255, 0, 255);

	if (inputTexture.wrap_mode == 0)
	{
		if (coord.X < 0) coord.X = 0;
		if (coord.Y < 0) coord.Y = 0;
		if (coord.X >= inputTexture.width) coord.X = inputTexture.width - 1;
		if (coord.Y >= inputTexture.height) coord.Y = inputTexture.height - 1;
	}
	else if (inputTexture.wrap_mode == 1)
	{
		if (coord.X < 0) coord.X = coord.X % inputTexture.width + inputTexture.width;
		if (coord.Y < 0) coord.Y = coord.Y % inputTexture.height + inputTexture.height;

		if (coord.X >= inputTexture.width) coord.X = coord.X % inputTexture.width;
		if (coord.Y >= inputTexture.height) coord.Y = coord.Y % inputTexture.height;
	}
	else
	{
		if (coord.X < 0 || coord.Y < 0 || coord.X >= inputTexture.width || coord.Y >= inputTexture.height) return (byte4)inputTexture.wrap_mode_color;
	}

	return *((byte4*)(inputTexture.TEXTURE_ADDR + inputTexture.width * coord.Y + coord.X));
}

inline byte4 texture(sampler2D inputTexture, vec2 coord, bool sampleAlpha = false)
{
	if (inputTexture.filt_mode == 0)
	{
		return textureNEAREST(inputTexture, int2((int)coord.x, (int)coord.y));
	}
	else
	{
		return textureBILINEAR(inputTexture, coord, sampleAlpha);
	}
}

template<typename T>
inline T textureNEAREST(sampler2D inputTexture, int2 coord)
{
	if (inputTexture.stride != sizeof(T))
		return T();

	if (inputTexture.wrap_mode == 0)
	{
		if (coord.X < 0) coord.X = 0;
		if (coord.Y < 0) coord.Y = 0;
		if (coord.X >= inputTexture.width) coord.X = inputTexture.width - 1;
		if (coord.Y >= inputTexture.height) coord.Y = inputTexture.height - 1;
	}
	else if (inputTexture.wrap_mode == 1)
	{
		if (coord.X < 0) coord.X = coord.X % inputTexture.width + inputTexture.width;
		if (coord.Y < 0) coord.Y = coord.Y % inputTexture.height + inputTexture.height;

		if (coord.X >= inputTexture.width) coord.X = coord.X % inputTexture.width;
		if (coord.Y >= inputTexture.height) coord.Y = coord.Y % inputTexture.height;
	}
	else
	{
		if (coord.X < 0 || coord.Y < 0 || coord.X >= inputTexture.width || coord.Y >= inputTexture.height) return T();
	}

	//void* ptr = (inputTexture.TEXTURE_ADDR + inputTexture.width * coord.Y + coord.X);

	return *((T*)(inputTexture.TEXTURE_ADDR + inputTexture.width * coord.Y + coord.X));
}

template<typename T>
inline T textureBILINEAR(sampler2D inputTexture, vec2 coord)
{
	if (inputTexture.stride != sizeof(T))
		return T();

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

	T bptrL = *(inputTexture.TEXTURE_ADDR + X0Y0.Y * inputTexture.width + X0Y0.X);
	T bptrLN = *(inputTexture.TEXTURE_ADDR + X1Y0.Y * inputTexture.width + X1Y0.X);

	T bptrU = *(inputTexture.TEXTURE_ADDR + X0Y1.Y * inputTexture.width + X0Y1.X);
	T bptrUN = *(inputTexture.TEXTURE_ADDR + X1Y1.Y * inputTexture.width + X1Y1.X);

	return bptrL * (x0 * y0) + bptrLN * (x1 * y0) + bptrU * (x0 * y1) + bptrUN * (x1 * y1);
}

template<typename T>
inline T texture(sampler2D inputTexture, int2 coord)
{
	if (inputTexture.filt_mode == 0)
	{
		return textureNEAREST<T>(inputTexture, int2((int)coord.x, (int)coord.y));
	}
	else
	{
		return textureBILINEAR<T>(inputTexture, coord);
	}
}

template<typename T>
inline void textureWrite(sampler2D inputTexture, int2 coord, T value)
{
	if (inputTexture.stride != sizeof(T))
		return;

	if (coord.X < 0) coord.X = 0;
	if (coord.Y < 0) coord.Y = 0;
	if (coord.X >= inputTexture.width) coord.X = inputTexture.width - 1;
	if (coord.Y >= inputTexture.height) coord.Y = inputTexture.height - 1;

	*(((T*)inputTexture.TEXTURE_ADDR) + inputTexture.width * coord.Y + coord.X) = value;
}

inline float texture(sampler1D inputBuffer, int index)
{
	if (index < 0) index = 0;
	if (index >= inputBuffer.size) index = inputBuffer.size - 1;

	return inputBuffer.mem_addr[index];
}


struct GLMatrix
{
	float nearZ;
	float farZ;

	float rw;
	float rh;

	float fw;
	float fh;

	float ox;
	float oy;

	float iox;
	float ioy;

	float oValue;
	float matrixlerpv;

	float fwi;
	float fhi;

	vec3 operator*(const vec3& coordinate) const
	{
		if (matrixlerpv == 0)
		{
			if (coordinate.z == 0)
				return vec3(0, 0, 0);

			float x = roundf(rw + coordinate.x / coordinate.z * fw);
			float y = roundf(rh + coordinate.y / coordinate.z * fh);
			
			return vec3(x, y, coordinate.z);
		}
		else if (matrixlerpv == 1)
		{
			float x = roundf(rw + coordinate.x * iox);
			float y = roundf(rh + coordinate.y * ioy);
			return vec3(x, y, coordinate.z);
		}
		else
		{
			float x = roundf(rw + coordinate.x / ((coordinate.z * fwi - ox) * (1.0f - matrixlerpv) + ox));
			float y = roundf(rh + coordinate.y / ((coordinate.z * fhi - oy) * (1.0f - matrixlerpv) + oy));
			return vec3(x, y, coordinate.z);
		}
	}

    vec3 ScreenToCameraSpace(float posX, float posY, float depth)
	{
		float X = ((depth * fwi - ox) * (1.0f - matrixlerpv) + ox) * (posX - rw);
		float Y = ((depth * fhi - oy) * (1.0f - matrixlerpv) + oy) * (posY - rh);

		return vec3(X, Y, depth);
	}
};


inline vec3 abs(vec3 val)
{
	return vec3(val.x >= 0 ? val.x : -val.x, val.y >= 0 ? val.y : -val.y, val.z >= 0 ? val.z : -val.z);
}

inline vec2 Cubemap_UVFace(vec3 dir, int& faceIndex)
{
	//FACE_INDEX_VALUE
	// RIGHT = INDEX 0
	// LEFT = INDEX 1
	// TOP = INDEX 2
	// BOTTOM = INDEX 3
	// FRONT = INDEX 4
	// BACK = INDEX 5

	vec3 vAbs = abs(dir);

	float ma;
	vec2 uv;
	if (vAbs.z >= vAbs.x && vAbs.z >= vAbs.y)
	{
		faceIndex = dir.z < 0 ? 5 : 4;
		ma = 0.5 / vAbs.z;
		uv = vec2(dir.z < 0.0 ? -dir.x : dir.x, -dir.y);
	}
	else if (vAbs.y >= vAbs.x)
	{
		faceIndex = dir.y < 0 ? 3 : 2;
		ma = 0.5 / vAbs.y;
		uv = vec2(dir.x, dir.y < 0 ? -dir.z : dir.z);
	}
	else
	{
		faceIndex = dir.x < 0 ? 1 : 0;
		ma = 0.5 / vAbs.x;
		uv = vec2(dir.x < 0 ? dir.z : -dir.z, -dir.y);
	}


	return uv * ma + vec2(0.5, 0.5);
}

inline byte4 textureNEAREST(samplerCube inputCubemap, vec3 dir)
{
	//FACE_INDEX_VALUE
	// RIGHT = INDEX 0
	// LEFT = INDEX 1
	// TOP = INDEX 2
	// BOTTOM = INDEX 3
	// FRONT = INDEX 4
	// BACK = INDEX 5

	int face;
	vec2 uv = Cubemap_UVFace(dir, face);

	if (uv.x > 1) uv.x = 1;
	else if (uv.x < 0) uv.x = 0;

	if (uv.y > 1) uv.y = 1;
	else if (uv.y < 0) uv.y = 0;


	int X = uv.x * (inputCubemap.width - 1);
	int Y = uv.y * (inputCubemap.height - 1);


	if (face == 0)
	{
		return inputCubemap.right[X + Y * inputCubemap.width];
	}
	else if (face == 1)
	{
		return inputCubemap.left[X + Y * inputCubemap.width];
	}
	else if (face == 2)
	{
		return inputCubemap.top[X + Y * inputCubemap.width];
	}
	else if (face == 3)
	{
		return inputCubemap.bottom[X + Y * inputCubemap.width];
	}
	else if (face == 4)
	{
		return inputCubemap.front[X + Y * inputCubemap.width];
	}
	else if (face == 5)
	{
		return inputCubemap.back[X + Y * inputCubemap.width];
	}

}

inline byte4 texture(samplerCube inputCubemap, vec3 dir)
{
	//FACE_INDEX_VALUE
	// RIGHT = INDEX 0
	// LEFT = INDEX 1
	// TOP = INDEX 2
	// BOTTOM = INDEX 3
	// FRONT = INDEX 4
	// BACK = INDEX 5

	int face;
	vec2 uv = Cubemap_UVFace(dir, face);

	if (uv.x > 1) uv.x = 1;
	else if (uv.x < 0) uv.x = 0;

	if (uv.y > 1) uv.y = 1;
	else if (uv.y < 0) uv.y = 0;


	int X = uv.x * (inputCubemap.width - 1);
	int Y = uv.y * (inputCubemap.height - 1);

	sampler2D t = sampler2D();
	t.filt_mode = inputCubemap.filt_mode;
	t.wrap_mode = inputCubemap.wrap_mode;
	t.wrap_mode_color = inputCubemap.wrap_mode_color;
	t.width = inputCubemap.width;
	t.height = inputCubemap.height;

	if (face == 0)
	{
		t.TEXTURE_ADDR = inputCubemap.right;
	}
	else if (face == 1)
	{
		t.TEXTURE_ADDR = inputCubemap.left;
	}
	else if (face == 2)
	{
		t.TEXTURE_ADDR = inputCubemap.top;
	}
	else if (face == 3)
	{
		t.TEXTURE_ADDR = inputCubemap.bottom;
	}
	else if (face == 4)
	{
		t.TEXTURE_ADDR = inputCubemap.front;
	}
	else if (face == 5)
	{
		t.TEXTURE_ADDR = inputCubemap.back;
	}

	return texture(t, vec2(X, Y));
}




struct MSAAConfig
{
	byte4** ptrPtrs;
	int sampleCount;
	float* sampleBuffer;
	float sampleMultiply;
};

inline float OnLineValue(float* A, float* B, float Cx, float Cy)
{
	return (B[0] - A[0]) * (Cy - A[1]) - (B[1] - A[1]) * (Cx - A[0]);
}

inline bool IsInside(float x, float y, float* VERTEX_DATA, int DATA_SIZE, int Stride)
{
	for (int i = 0; i < DATA_SIZE - 1; i++)
	if (OnLineValue(VERTEX_DATA + i * Stride, VERTEX_DATA + (i + 1) * Stride, x, y) < 0)
		return false;

	if (OnLineValue(VERTEX_DATA + (DATA_SIZE - 1) * Stride, VERTEX_DATA + 0 * Stride, x, y) < 0)
		return false;

	return true;
}

inline void MSAA_SAMPLE(byte4 data, int RW, int X, int Y, int stride, float* VERTEX_DATA, int DATA_SIZE, MSAAConfig& mConfig)
{
	//Dear compiler: please unroll the loops!
	if (mConfig.sampleCount == 2)
	{
		for (int i = 0; i < 2; i++)
		{
			float X_SAMPLE = X + mConfig.sampleBuffer[i * 2 + 0];
			float Y_SAMPLE = Y + mConfig.sampleBuffer[i * 2 + 1];

			if (IsInside(X_SAMPLE, Y_SAMPLE, VERTEX_DATA, DATA_SIZE, stride))
			{
				(mConfig.ptrPtrs[i])[RW * Y + X] = data;
			}
		}
	}
	else if (mConfig.sampleCount == 4)
	{
		for (int i = 0; i < 4; i++)
		{
			float X_SAMPLE = X + mConfig.sampleBuffer[i * 2 + 0];
			float Y_SAMPLE = Y + mConfig.sampleBuffer[i * 2 + 1];

			if (IsInside(X_SAMPLE, Y_SAMPLE, VERTEX_DATA, DATA_SIZE, stride))
			{
				(mConfig.ptrPtrs[i])[RW * Y + X] = data;
			}
		}
	}
	else if (mConfig.sampleCount == 8)
	{
		for (int i = 0; i < 8; i++)
		{
			float X_SAMPLE = X + mConfig.sampleBuffer[i * 2 + 0];
			float Y_SAMPLE = Y + mConfig.sampleBuffer[i * 2 + 1];

			if (IsInside(X_SAMPLE, Y_SAMPLE, VERTEX_DATA, DATA_SIZE, stride))
			{
				(mConfig.ptrPtrs[i])[RW * Y + X] = data;
			}
		}
	}
}



