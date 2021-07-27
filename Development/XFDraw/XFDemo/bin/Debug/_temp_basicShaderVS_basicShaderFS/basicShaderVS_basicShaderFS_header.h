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
	long* TEXTURE_ADDR;
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
