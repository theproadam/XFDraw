struct vec3
{
	float x;
	float y;
	float z;

	vec3(float X, float Y, float Z)
	{
		x = X;
		y = Y;
		z = Z;
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

	vec4(vec3 Vector3, float wValue)
	{
		x = Vector3.x;
		y = Vector3.y;
		z = Vector3.z;
		w = wValue;
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

byte4 textureNEAREST(sampler2D inputTexture, int2 coord);
byte4 textureBLINEAR(sampler2D inputTexture, vec2 coord);
//inline void RotateVertex(vec3* XYZ_IN, vec3* XYZ_OUT, vec3 angleCOS, vec3 angleSIN, vec3 aroundPosition);
vec3 RotateVertex(vec3* XYZ_IN, vec3 angleCOS, vec3 angleSIN, vec3 aroundPosition);

extern int TEXTURE_WRAP_MODE;
extern byte4 TEXTURE_CLAMP_BORDER_COLOR;
extern bool rmq;
extern int qmr;
extern long* taddr;


template <typename VS, typename FS>
void RegisterShader(void* VS_ADDR, void* FS_ADDR)
{
	if (rmq)
	{
		taddr[qmr + 0] = sizeof(VS);
		taddr[qmr + 1] = (long)VS_ADDR;
		taddr[qmr + 2] = sizeof(FS);
		taddr[qmr + 3] = (long)FS_ADDR;
		qmr += 4;
	}
	else qmr++;
}

template <typename SSShader>
void RegisterShader(void* SSS_ADDR)
{
	if (rmq)
	{
		taddr[qmr + 0] = -1;
		taddr[qmr + 1] = -1;
		taddr[qmr + 2] = sizeof(SSShader);
		taddr[qmr + 3] = (long)SSS_ADDR;
		qmr += 4;
	}
	else qmr++;
}

template <typename VS, typename FS>
void RegisterBasicShader(void* VS_ADDR, void* FS_ADDR)
{
	
}

template <typename SSShader>
void RegisterBasicShader(void* SSS_ADDR)
{

}


void LoadModule();

#define uniform
#define in
#define out
#define variable
#define layout( a2 )

#define VS_out


//Declare a shader
#define DeclareShader(ShaderName, StuctName, EntryPoint) void ShaderName ## Trigger(StuctName* ptr) { ptr->EntryPoint(); } void* ShaderName = &ShaderName ## Trigger;



#define GL_CLAMP_TO_EDGE 0
#define GL_REPEAT 1
#define GL_CLAMP_TO_BORDER 3