#include "stdafx.h"
#include "xfcore.h"

struct FS3D
{
	//out
	out byte4* FragColor;
	
	//in
	//in vec3* normals;
	in vec2* UV;

	//uniform vec3 cameraWorld;
	//uniform sampler2D ourTexture;

	void main()
	{
		//*FragColor = byte4(UV->x * 255.0f, UV->y * 255.0f, 0);
	}
};


struct VS3D
{
	layout (location = 0) in vec3* coords;
	layout (location = 1) in vec2* uv_in;

	VS_out vec3* gl_Position;
	out vec2* uv_out;

	uniform vec3 camPos;
	uniform vec3 rotCos;
	uniform vec3 rotSin;

	inline void main()
	{
		//RotateVertex(coords, coords_out, rotCos, rotSin, camPos);
	//	*gl_Position =
		//	RotateVertex(coords, rotCos, rotSin, camPos);

	//	RotateVertex(coords, camPos, camPos, camPos);
		
	//	*uv_out = *uv_in;;
	}
};

struct ColorShift
{
	out byte4* outColor;
	uniform byte4 tcolor;

	void main()
	{
		*outColor = tcolor;
	}
};

struct CShift
{
	out byte4* outColor; 
	
	variable int2 XY_Coords;
	uniform vec2 viewportMod;

	void main()
	{	
		float X = (XY_Coords.X * viewportMod.x);
		float Y = (XY_Coords.Y * viewportMod.y);

		outColor->R = X;
		outColor->G = Y;
	}
};

struct CreateVingetteBuffer
{
	out float* outMultiplier;

	variable int2 XY_Coords;

	uniform vec2 viewportMod;

	void main()
	{
		float X = (XY_Coords.X * viewportMod.x) - 1.0f;
		float Y = (XY_Coords.Y * viewportMod.y) - 1.0f;

		X = 1.0f - 0.5f * X * X;
		Y = X * (1.0f - 0.5f * Y * Y);

		*outMultiplier = Y;
	}
};

struct MultiplyBy
{
	out byte4* outColor;
	in float* inMultiplier;

	void main()
	{
		outColor->R *= *inMultiplier;
		outColor->G *= *inMultiplier;
		outColor->B *= *inMultiplier;
	}
};

const float xw = 384.0f / 1600.0f;
const float yh = 384.0f / 900.0f;


struct DisplayTexture
{
	out byte4* outColor;

	variable int2 XY_Coords;
	uniform vec2 viewportMod;
	uniform sampler2D sourceTexture;

	void main()
	{
		// int2 pos = int2(XY_Coords.X * viewportMod.x * sourceTexture.width - 50, XY_Coords.Y * viewportMod.y * sourceTexture.height - 50);
		 int2 pos = int2((float)XY_Coords.X * xw - 128.0f, (float)XY_Coords.Y * yh - 128.0f);


		 *outColor = textureNEAREST(sourceTexture, pos);

		//vec2 pos = vec2(XY_Coords.X * xw - 128.0f, XY_Coords.Y * yh - 128.0f);

		//*outColor = textureBLINEAR(sourceTexture, pos);

	}
};



void ExecuteFS(FS3D* ptr)
{
	ptr->main();
}

void ExecuteVS(VS3D* ptr)
{
	ptr->main();
}


DeclareShader(ExecuteSSS, CShift, main)
DeclareShader(BuildVignette, CreateVingetteBuffer, main)
DeclareShader(MultiplyVignette, MultiplyBy, main)
DeclareShader(ColorShifter, ColorShift, main)
DeclareShader(DTexture, DisplayTexture, main)



void BasicSS(unsigned char* BGR, int posX, int posY, void* UniformData)
{
	
}

void BasicVS(float* input, float* output, void* structData)
{
	
}

void BasicVS(byte4* color, void* structData)
{

}


void LoadModule()
{
	RegisterShader<VS3D, FS3D>(&ExecuteVS, &ExecuteFS);
	RegisterShader<CShift>(ExecuteSSS);
	RegisterShader<CreateVingetteBuffer>(BuildVignette);
	RegisterShader<MultiplyBy>(MultiplyVignette);
	RegisterShader<ColorShift>(ColorShifter);
	RegisterShader<DisplayTexture>(DTexture);

	TEXTURE_WRAP_MODE = GL_CLAMP_TO_EDGE;
}
