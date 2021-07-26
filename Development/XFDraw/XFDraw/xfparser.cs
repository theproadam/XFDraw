using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using xfcore.Shaders;

namespace xfcore.Shaders.Builder
{
    public class ShaderCompile
    {
        public static string COMPILER_LOCATION = @"C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\bin\";
        public static string COMPILER_NAME = "cl.exe";
        public static string COMMAND_LINE = "/openmp /nologo /GS /GL /Zc:forScope /Oi /MD -ffast-math /O2 /fp:fast -Ofast /Oy /Og /Ox /Ot";

        ShaderField[] sFieldsVS;
        ShaderField[] sUniformsVS;
        ShaderMethod[] sMethodsVS;
        ShaderStruct[] sStructsVS;

        ShaderField[] sFieldsFS;
        ShaderField[] sUniformsFS;
        ShaderMethod[] sMethodsFS;
        ShaderStruct[] sStructsFS;

        bool skipCompile = false;
        bool isScreenSpace = false;
        string shaderName;

        internal ShaderCompile(ShaderParser VS, ShaderParser FS, bool isScreen, string sName, bool skipcompile)
        {
            if (!isScreen)
            {
                sFieldsVS = VS.shaderFields;
                sUniformsVS = VS.shaderUniforms;
                sMethodsVS = VS.shaderMethods;
                sStructsVS = VS.shaderStructs;
            }

            sFieldsFS = FS.shaderFields;
            sUniformsFS = FS.shaderUniforms;
            sMethodsFS = FS.shaderMethods;
            sStructsFS = FS.shaderStructs;


            isScreenSpace = isScreen;
            shaderName = sName;

            skipCompile = skipcompile;
        }

        public string PrintVertexShader()
        {
            if (isScreenSpace)
                throw new Exception("This is a screenspace shader!");

            string str = "";

            for (int i = 0; i < sFieldsVS.Length; i++)
                str += sFieldsVS[i].dataMode.ToString() + " " + sFieldsVS[i].dataType.ToString() + " " + sFieldsVS[i].name + ";\n";

            str += "\n";

            for (int i = 0; i < sStructsVS.Length; i++)
            {
                str += "struct " + sStructsVS[i].structName + " {";

                for (int j = 0; j < sStructsVS[i].structFields.Length; j++)
                {
                    str += "\n\t" + sStructsVS[i].structFields[j].dataType.ToString() + " " + sStructsVS[i].structFields[j].name + ";";
                }
                str += "\n};\n";
            }

            str += "\n";

            for (int i = 0; i < sMethodsVS.Length; i++)
            {
                string nstr = "\t" + Regex.Replace(sMethodsVS[i].contents, ";", ";\n\t");
                str += sMethodsVS[i].entryName + "\n{\n" + nstr.Substring(0, nstr.Length - 1) + "}\n\n";
            }

            return str;
        }

        public string PrintFragmentShader()
        {
            if (isScreenSpace)
                throw new Exception("This is a screenspace shader!");

            string str = "";

            for (int i = 0; i < sFieldsFS.Length; i++)
                str += sFieldsFS[i].dataMode.ToString() + " " + sFieldsFS[i].dataType.ToString() + " " + sFieldsFS[i].name + ";\n";

            str += "\n";

            for (int i = 0; i < sStructsFS.Length; i++)
            {
                str += "struct " + sStructsFS[i].structName + " {";

                for (int j = 0; j < sStructsFS[i].structFields.Length; j++)
                {
                    str += "\n\t" + sStructsFS[i].structFields[j].dataType.ToString() + " " + sStructsFS[i].structFields[j].name + ";";
                }
                str += "\n};\n";
            }

            str += "\n";

            for (int i = 0; i < sMethodsFS.Length; i++)
            {
                string nstr = "\t" + Regex.Replace(sMethodsFS[i].contents, ";", ";\n\t");
                str += sMethodsFS[i].entryName + "\n{\n" + nstr.Substring(0, nstr.Length - 1) + "}\n\n";
            }

            return str;
        }

        public string PrintScreenSpaceShader()
        {
            if (!isScreenSpace)
                throw new Exception("Not a screenspace shader!");

            string str = "";

            for (int i = 0; i < sFieldsFS.Length; i++)
                str += sFieldsFS[i].dataMode.ToString() + " " + sFieldsFS[i].dataType.ToString() + " " + sFieldsFS[i].name + ";\n";

            str += "\n";

            for (int i = 0; i < sStructsFS.Length; i++)
            {
                str += "struct " + sStructsFS[i].structName + " {";

                for (int j = 0; j < sStructsFS[i].structFields.Length; j++)
                {
                    str += "\n\t" + sStructsFS[i].structFields[j].dataType.ToString() + " " + sStructsFS[i].structFields[j].name + ";";
                }
                str += "\n};\n";
            }

            str += "\n";

            for (int i = 0; i < sMethodsFS.Length; i++)
            {
                string nstr = "\t" + Regex.Replace(sMethodsFS[i].contents, ";", ";\n\t");
                str += sMethodsFS[i].entryName + "\n{\n" + nstr.Substring(0, nstr.Length - 1) + "}\n\n";
            }

            return str;
        }

        public bool Compile(out Shader compiledShader)
        {
            compiledShader = null;

            string folder = "_temp_" + shaderName;

            if (skipCompile)
            {
                compiledShader = new Shader(folder + @"\" + shaderName + "_merged.dll", this);
                return true;
            }

            string cppFileSource = shaderName + "_merged.cpp";
            string headerFile = shaderName + "_header.h";



            if (!File.Exists(folder + @"\" + cppFileSource) || !File.Exists(folder + @"\" + headerFile))
                throw new FileNotFoundException("Missing File!");

            string path = System.AppDomain.CurrentDomain.BaseDirectory;
            string tempPath = path + folder + "\\";// +"_temp_" + cpp;
            string outdir = tempPath + "\\" + shaderName + "_merged.dll";

            if (File.Exists(outdir))
                File.Delete(outdir);

            Process compiler = new Process();

            compiler.StartInfo.FileName = "cmd.exe";
            compiler.StartInfo.WorkingDirectory = tempPath;
            compiler.StartInfo.RedirectStandardInput = true;
            compiler.StartInfo.RedirectStandardOutput = true;
            compiler.StartInfo.UseShellExecute = false;
            
            compiler.Start();

            compiler.StandardInput.WriteLine("\"" + @"C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\bin\vcvars32.bat" + "\"");
            compiler.StandardInput.WriteLine(@"cl.exe /EHsc /LD " + cppFileSource + " " + COMMAND_LINE);
            compiler.StandardInput.WriteLine(@"exit");


            string[] outputFile = cppFileSource.Split('.');

            compiler.WaitForExit();
           
            if (!File.Exists(outdir))
            {
                Console.WriteLine("Failed To Compile: ");
                string output = compiler.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
                return false;
            }

            compiler.Close();


            compiledShader = new Shader(folder + @"\" + shaderName + "_merged.dll", this);

            return true;
        }

        internal ShaderField[] PrepareUniformsFS(out int Size)
        {
            List<ShaderField> uniforms = new List<ShaderField>();

            for (int i = 0; i < sUniformsFS.Length; i++)
                if (sUniformsFS[i].dataMode == DataMode.Uniform)
                    uniforms.Add(sUniformsFS[i]);

            int offset = 0;
            for (int i = 0; i < uniforms.Count; i++)
            {
                uniforms[i].layoutPosition = offset;
                offset += uniforms[i].GetSize();
            }

            Size = offset;
            return uniforms.ToArray();
        }

        internal ShaderField[] PrepareUniformsVS(out int Size)
        {
            List<ShaderField> uniforms = new List<ShaderField>();

            for (int i = 0; i < sUniformsVS.Length; i++)
                if (sUniformsVS[i].dataMode == DataMode.Uniform)
                    uniforms.Add(sUniformsVS[i]);

            int offset = 0;
            for (int i = 0; i < uniforms.Count; i++)
            {
                uniforms[i].SetLayoutOffset(offset);
                offset += uniforms[i].GetSize();
            }

            Size = offset;
            return uniforms.ToArray();
        }

        internal ShaderField[] GetFieldFS()
        {
            return sFieldsFS;
        }


    }

    public class ShaderParser
    {
        internal ShaderField[] shaderFields;
        internal ShaderField[] shaderUniforms;
        internal ShaderMethod[] shaderMethods;
        internal ShaderStruct[] shaderStructs;

        public static string HeaderFile
        {
            get
            {
                return @"#include <cstdint>
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

	vec3()
	{
		x = 0;
		y = 0;
		z = 0;
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

void fcpy(char* dest, char* src, int count)
{
	for (int i = 0; i < count; ++i)
		dest[i] = src[i];
}

extern " + "\"C\"" + @" __declspec(dllexport) int32_t CheckSize()
{
	return sizeof(void*);
}

";
            }
        }

        public static string ClippingCode
        {
            get { return @"#pragma region NearPlaneCFG

	int v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * stride + 2] < projData.nearZ)
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return RETURN_VALUE;

#pragma endregion

#pragma region NearPlane
	if (v != 0)
	{
		float* strFLT = (float*)alloca((BUFFER_SIZE * stride + stride) * 4);

		int API = 0;

		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.nearZ, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, projData.nearZ, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.nearZ, stride);

					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else if (i > 0 && AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, projData.nearZ, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
			}
		}

		BUFFER_SIZE = API / stride;
		VERTEX_DATA = strFLT;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}

#pragma endregion

#pragma region FarPlaneCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * stride + 2] > projData.farZ)
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return RETURN_VALUE;

#pragma endregion

#pragma region FarPlane
	if (v != 0)
	{
		float* strFLT = (float*)alloca((BUFFER_SIZE * stride + stride) * 4);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.farZ, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, projData.farZ, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.farZ, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else if (i > 0 && AP[i - 1])
				{
					FIPA(strFLT, API, VERTEX_DATA, i - 1, i, projData.farZ, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / stride;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}
#pragma endregion

#pragma region RightFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * stride + 2] * projData.tanVert + projData.ow < VERTEX_DATA[i * stride])
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return RETURN_VALUE;
#pragma endregion

#pragma region RightFOV
	if (v != 0)
	{
		float* strFLT = (float*)alloca((BUFFER_SIZE * stride + stride) * 4);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.tanVert, projData.ow, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, projData.tanVert, projData.ow, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.tanVert, projData.ow, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, projData.tanVert, projData.ow, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / stride;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}
#pragma endregion

#pragma region LeftFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * stride + 2] * -projData.tanVert - projData.ow > VERTEX_DATA[i * stride])
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return RETURN_VALUE;
#pragma endregion

#pragma region LeftFOV
	if (v != 0)
	{
		float* strFLT = (float*)alloca((BUFFER_SIZE * stride + stride) * 4);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -projData.tanVert, -projData.ow, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -projData.tanVert, -projData.ow, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -projData.tanVert, -projData.ow, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -projData.tanVert, -projData.ow, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / stride;
		RtlZeroMemory(AP, BUFFER_SIZE);
	}
#pragma endregion

#pragma region TopFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * stride + 2] * projData.tanHorz + projData.oh < VERTEX_DATA[i * stride + 1])
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return RETURN_VALUE;

#pragma endregion

#pragma region TopFOV

	if (v != 0)
	{
		float* strFLT = (float*)alloca((BUFFER_SIZE * stride + stride) * 4);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.tanHorz, projData.oh, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, projData.tanHorz, projData.oh, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, projData.tanHorz, projData.oh, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, projData.tanHorz, projData.oh, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / stride;
		RtlZeroMemory(AP, BUFFER_SIZE);


	}

#pragma endregion

#pragma region BottomFOVCFG
	v = 0;

	for (int i = 0; i < BUFFER_SIZE; i++)
	{
		if (VERTEX_DATA[i * stride + 2] * -projData.tanHorz - projData.oh > VERTEX_DATA[i * stride + 1])
		{
			AP[i] = true;
			v++;
		}
	}

	if (v == BUFFER_SIZE)
		return RETURN_VALUE;

#pragma endregion

#pragma region BottomFOV
	if (v != 0)
	{
		float* strFLT = (float*)alloca((BUFFER_SIZE * stride + stride) * 4);
		int API = 0;
		for (int i = 0; i < BUFFER_SIZE; i++)
		{
			if (AP[i])
			{
				if (i == 0 && !AP[BUFFER_SIZE - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -projData.tanHorz, -projData.oh, stride);
					API += stride;
				}
				else if (i > 0 && !AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -projData.tanHorz, -projData.oh, stride);
					API += stride;
				}
			}
			else
			{
				if (i == 0 && AP[BUFFER_SIZE - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -projData.tanHorz, -projData.oh, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];


					API += stride;
				}
				else if (i > 0 && AP[i - 1])
				{
					SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -projData.tanHorz, -projData.oh, stride);
					API += stride;

					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];


					API += stride;
				}
				else
				{
					strFLT[API + 0] = VERTEX_DATA[i * stride];
					strFLT[API + 1] = VERTEX_DATA[i * stride + 1];
					strFLT[API + 2] = VERTEX_DATA[i * stride + 2];

					for (int a = 3; a < stride; a++)
						strFLT[API + a] = VERTEX_DATA[i * stride + a];

					API += stride;
				}
			}
		}
		VERTEX_DATA = strFLT;
		BUFFER_SIZE = API / stride;
	}
#pragma endregion"; }
        }

        ShaderParser(ShaderField[] f, ShaderField[] u, ShaderMethod[] m, ShaderStruct[] s)
        {
            shaderFields = f;
            shaderUniforms = u;
            shaderMethods = m;
            shaderStructs = s;
        }

        public static bool Parse(string vertexShader, string fragmentShader, out ShaderCompile shaderModule, params CompileOption[] compileOptions)
        {
            if (!File.Exists(vertexShader) || !File.Exists(fragmentShader))
                throw new FileNotFoundException();

            string[] analyzeVS = File.ReadAllLines(vertexShader);
            string VSReady = PrepareInput(analyzeVS);
            ShaderParser vsModule = Parse(VSReady);

            string[] analyzeFS = File.ReadAllLines(fragmentShader);
            string FSReady = PrepareInput(analyzeFS);
            ShaderParser fsModule = Parse(FSReady);

            CheckForNonFloats(vsModule); //Check is done automatically during linkage checking
            CheckVSFSLinkage(vsModule, fsModule);
            
            string namePath = "";

            int readStride, iStride;

            WriteShaders(vertexShader, fragmentShader, vsModule, fsModule, compileOptions, out namePath, out readStride, out iStride);
            shaderModule = new ShaderCompile(vsModule, fsModule, false, namePath, false);

            return true;
        }

        public static bool Parse(string screenspaceShader, out ShaderCompile shaderModule, params CompileOption[] compileOptions)
        {
            if (!File.Exists(screenspaceShader))
                throw new FileNotFoundException();

            string[] analyzeS = File.ReadAllLines(screenspaceShader);
            string SReady = PrepareInput(analyzeS);
            ShaderParser sModule = Parse(SReady);

            string shaderName = "";

            bool skipcompile = !WriteShader(screenspaceShader, sModule, compileOptions, out shaderName);
            shaderModule = new ShaderCompile(null, sModule, true, shaderName, skipcompile);

            return true;
        }

        public static string PrepareInput(string[] str)
        {
            str = RemoveComments(str);
            string s = FlattenString(str);
            return RemoveBlockComments(s);
        }

        static void CheckVSFSLinkage(ShaderParser vs, ShaderParser fs)
        {
            int vFieldC = 0;
            int fFieldC = 0;

            for (int i = 0; i < vs.shaderFields.Length; i++)
            {
                if (vs.shaderFields[i].dataMode == DataMode.In) continue;
                int count = 0;
                fFieldC = 0;

                for (int j = 0; j < fs.shaderFields.Length; j++)
                {
                    if (fs.shaderFields[j].dataMode == DataMode.Out) continue;
                    fFieldC++;
                    if (vs.shaderFields[i].name == fs.shaderFields[j].name){
                        count++;
                        if (vs.shaderFields[i].GetSize() != fs.shaderFields[j].GetSize())
                            throw new Exception("The vs OUT \"" + vs.shaderFields[i].name + "\" is not the same size as the FS out");
                    }
                }

                vFieldC++;
                if (count == 0) throw new Exception("The vs OUT \"" + vs.shaderFields[i].name + "\" does not exist in the fs!");
                if (count > 1) throw new Exception("The vs OUT \"" + vs.shaderFields[i].name + "\" exists multiple times in the fs!");
            }

            if (vFieldC != fFieldC) throw new Exception("There seems to me a mismatch in the attribute count between the vs and fs!");
        }

        static void CheckForNonFloats(ShaderParser vs)
        {
            for (int i = 0; i < vs.shaderFields.Length; i++)
            {
                DataType d = vs.shaderFields[i].dataType;

                if (!(d == DataType.vec2 || d == DataType.vec3))
                    throw new Exception("The vertex shader can only work with floating point types!");
            }
        }

        static void WriteShaders(string filePath1, string filePath2, ShaderParser vs, ShaderParser fs, CompileOption[] cOps, out string wPath, out int readS, out int intS)
        {
            string ext = filePath1.Split('.')[0] + "_" + filePath2.Split('.')[0];
            wPath = ext;

            ShaderField[] vsIn, vsOut;
            PrepVSData(vs, out vsIn, out vsOut, out readS, out intS);

            bool forceC = cOps.Contains(CompileOption.ForceRecompile);

            string foldername = "_temp_" + ext;
            //realPath = foldername;

            #region Foldering

            if (!Directory.Exists(foldername))
                Directory.CreateDirectory(foldername);

            bool prevFile = false;

            if (File.Exists(foldername + @"\" + filePath1))
            {
                if (File.ReadLines(filePath1).SequenceEqual(File.ReadLines(foldername + @"\" + filePath1)) && !forceC)
                    prevFile = true;
                else
                    File.Delete(foldername + @"\" + filePath1);
            }

            if (forceC) prevFile = false;

            if (File.Exists(foldername + @"\" + filePath2))
            {
                if (File.ReadLines(filePath2).SequenceEqual(File.ReadLines(foldername + @"\" + filePath2)) && prevFile)
                    return;

                File.Delete(foldername + @"\" + filePath2);
            }

            File.Copy(filePath1, foldername + @"\" + filePath1);
            File.Copy(filePath2, foldername + @"\" + filePath2);

            #endregion

            if (!ContainsName(vs.shaderMethods[vs.shaderMethods.Length - 1].contents, "gl_Position"))
                throw new Exception("The vertex shader needs the gl_Position vector3 set!");

            string sign, exec;
            WriteVSSign(vs, vsIn, vsOut, out sign, out exec);

            string shaderCode = "";
            shaderCode += "const int stride = " + intS + ";\n";
            shaderCode += "const int readStride = " + readS + ";\n";
            shaderCode += "const int faceStride = " + (readS * 3) + ";\n\n";

            shaderCode += "float* VERTEX_DATA = (float*)alloca(stride * 3 * 4);\n";
            shaderCode += "int BUFFER_SIZE = 3;\n";

            shaderCode += "for (int b = 0; b < 3; ++b){\n\t";
            shaderCode += "float* input = p + (index * faceStride + b * readStride);\n\t";
            shaderCode += "float* output = VERTEX_DATA + b * stride;\n\t" + exec + "\n}\n\n";

            shaderCode += "bool* AP = (bool*)alloca(BUFFER_SIZE + 12);\n";
            shaderCode += "frtlzeromem(AP, BUFFER_SIZE);\n\n";



            Console.WriteLine("");
        }

        static void PrepVSData(ShaderParser data, out ShaderField[] vsIn, out ShaderField[] vsOut, out int readS, out int iS)
        {
            int count = 0;

            List<ShaderField> vsI = new List<ShaderField>();
            List<ShaderField> vsO = new List<ShaderField>();

            for (int i = 0; i < data.shaderFields.Length; i++){
                if (data.shaderFields[i].dataMode == DataMode.In) vsI.Add(data.shaderFields[i]);
                else if (data.shaderFields[i].dataMode == DataMode.Out) vsO.Add(data.shaderFields[i]);
                else throw new Exception("An error occured (9582)");
            }

            for (int i = 0; i < vsI.Count; i++)
                count += vsI[i].layoutValueGL != -1 ? 1 : 0;

            if (!(count == 0 || count == vsI.Count))
                throw new Exception("VS layout not set correctly!");

            if (count != 0){
                vsI.Sort(delegate(ShaderField x, ShaderField y) {
                    return x.layoutValueGL.CompareTo(y.layoutValueGL);
                });
            }

            int offset = 0;
            for (int i = 0; i < vsI.Count; i++)
            {
                vsI[i].layoutPosition = offset;
                offset += vsI[i].GetSize();
            }

            readS = offset;

            int offset1 = 0;
            for (int i = 0; i < vsO.Count; i++)
            {
                vsO[i].layoutPosition = offset1;
                offset1 += vsO[i].GetSize();
            }

            iS = offset1;

            vsIn = vsI.ToArray();
            vsOut = vsO.ToArray();
        }

        static void WriteVSSign(ShaderParser data, ShaderField[] vsIn, ShaderField[] vsOut, out string sign, out string exec)
        {
            string methodSign = "inline void VSExec(";
            string methodExec = "VSExec(";

            for (int i = 0; i < vsIn.Length; i++)
            {
                string type = TypeToString(vsIn[i].dataType);
                methodExec += "(" + type + "*)(input + " + (vsIn[i].layoutPosition / 4) + "), ";
                methodSign += type + "* " + vsIn[i].name + ", ";
            }

            methodExec += "(vec3*)(output + 0), ";
            methodSign += "vec3* gl_Position, ";

            for (int i = 0; i < vsOut.Length; i++)
            {
                string type = TypeToString(vsOut[i].dataType);
                methodExec += "(" + type + "*)(output + " + ((vsOut[i].layoutPosition / 4) + 3) + "), ";
                methodSign += type + "* " + vsOut[i].name + ", ";
            }

            for (int i = 0; i < data.shaderUniforms.Length; i++)
            {
                string type = data.shaderUniforms[i].dataType != DataType.Other ? TypeToString(data.shaderUniforms[i].dataType) : data.shaderUniforms[i].typeName;

                methodExec += "*(" + type + "*)(uniformData + " + data.shaderUniforms[i].layoutPosition + "), ";
                methodSign += type + " " + data.shaderUniforms[i].name + ", ";
            }


            exec = methodExec.Substring(0, methodExec.Length - 2) + ");";
            sign = methodSign.Substring(0, methodSign.Length - 2) + ")";          
        }

        static bool WriteShader(string filePath, ShaderParser data, CompileOption[] cOps, out string writtenPath)
        {
            string foldername = "_temp_" + filePath.Split('.')[0];

            writtenPath = filePath.Split('.')[0];

            if (!Directory.Exists(foldername))
                Directory.CreateDirectory(foldername);

            if (File.Exists(foldername + @"\" + filePath))
            {
                if (File.ReadLines(filePath).SequenceEqual(File.ReadLines(foldername + @"\" + filePath)) && !cOps.Contains(CompileOption.ForceRecompile))
                    if (File.Exists(foldername + @"\" + writtenPath + "_merged.dll"))
                        return false;        

                File.Delete(foldername + @"\" + filePath);
            }

            File.Copy(filePath, foldername + @"\" + filePath);

            List<string> ptrs = new List<string>();

            string ptrsIncr = ""; //Pointers that are incremented
            string methdSign = ""; //Method Signature for pointers

            string methdSignUnifm = ""; //Method signature for uniforms

            string indt = "\t\t", indt1 = "\t"; //indent level 1 & 2

            string uFields = ""; //uniform field and data copying

            string execSignPtr = ""; //inline void shaderMethod(float* INOUT)
            string execSignUni = ""; //inline void shaderMethod(vec3 UNIFORM)

            if (!data.shaderMethods[data.shaderMethods.Length - 1].isEntryPoint)
                throw new Exception("No void main() shader entry point detected!");

            string mainCode = data.shaderMethods[data.shaderMethods.Length - 1].contents;
            string structDeclrs = "";

            ShaderField[] uniforms = data.shaderUniforms;
            ShaderField[] inoutFields = data.shaderFields;
            string methods = WriteMethods(data.shaderMethods, inoutFields, uniforms);

            for (int i = 0; i < uniforms.Length; i++)
            {
                if (uniforms[i].dataType == DataType.Other)
                    structDeclrs += "struct " + uniforms[i].typeName + " {\n" + uniforms[i].typeAlt.data + "\n};";

                string type = uniforms[i].dataType != DataType.Other ? TypeToString(uniforms[i].dataType) : uniforms[i].typeName;
                int size = uniforms[i].dataType != DataType.Other ? TypeToSize(uniforms[i].dataType) : uniforms[i].FieldSize;

                if (size == -1) throw new Exception("A parsing error occured! (10512)");

                uFields += indt1 + type + " uniform_" + i + ";\n";
                uFields += indt1 + "fcpy((char*)(&uniform_" + i + "), (char*)UniformPointer + " + uniforms[i].layoutPosition + ", " + size + ");\n";

                methdSignUnifm += "uniform_" + i + ", ";
                execSignUni += type + " " + uniforms[i].name + ", ";
            }

            for (int i = 0; i < inoutFields.Length; i++)
            {
                if (inoutFields[i].dataType == DataType.Other) throw new Exception("Custom structs cannot be used as in/out fields!");

                string type = TypeToString(inoutFields[i].dataType);

                ptrs.Add(indt + type + "* " + "ptr_" + i + " = (" + type + "*)(ptrPtrs[" + i + "] + wPos * " + TypeToSize(inoutFields[i].dataType) + ");\n");
                ptrsIncr += ", ++ptr_" + i;
                methdSign += "ptr_" + i + ", ";
                execSignPtr += type + "* " + inoutFields[i].name + ", ";
            }

            mainCode = Regex.Replace(mainCode, ";", ";\n\t");
            mainCode = Regex.Replace(mainCode, "{", "{\n\t");
            mainCode = "\t" + Regex.Replace(mainCode, "}", "}\n\t");

            for (int i = 0; i < inoutFields.Length; i++)
                mainCode = WrapVariablePointer(mainCode, inoutFields[i].name);

            bool XYReq = ContainsName(mainCode, "gl_FragCoord");

            string methodSignExec = execSignPtr + execSignUni;

            if (XYReq)
                methodSignExec += "vec3 gl_FragCoord, ";

            if (methodSignExec.Length > 2)
                methodSignExec = methodSignExec.Substring(0, methodSignExec.Length - 2);

            List<string> shaderOutput = new List<string>();
            shaderOutput.Add("//Autogenerated by XFDraw shader parser");
            shaderOutput.Add("#include \"" + writtenPath + "_header.h" + "\"\n");

            if (structDeclrs != "") shaderOutput.Add(structDeclrs);
            if (methods != "") shaderOutput.Add(methods);  

            shaderOutput.Add("inline void shaderMethod(" + methodSignExec + "){\n" + mainCode + "\n}");

            //Add Base Part
            const string entry = "extern \"C\" __declspec(dllexport) void ";
            shaderOutput.Add(entry + "ShaderCallFunction(long Width, long Height, unsigned char** ptrPtrs, void* UniformPointer){");

            shaderOutput.Add(uFields);

            shaderOutput.Add("#pragma omp parallel for");
            shaderOutput.Add("\tfor (int h = 0; h < Height; ++h){");
            shaderOutput.Add("\t\tint wPos = Width * h;");
            shaderOutput.AddRange(ptrs);

            string methodSignPre = methdSign + methdSignUnifm;

            if (XYReq)
            {
                shaderOutput.Add("\t\tvec3 gl_FragCoord = vec3(0, h, 0);");
                methodSignPre += "gl_FragCoord, ";
                ptrsIncr += ", ++gl_FragCoord.x";
            }


            if (methodSignPre.Length > 2)
                methodSignPre = methodSignPre.Substring(0, methodSignPre.Length - 2);


            shaderOutput.Add("\t\tfor (int w = 0; w < Width; ++w" + ptrsIncr + "){");
            shaderOutput.Add("\t\t\tshaderMethod(" + methodSignPre + ");\n\t\t}\n\t}");

            shaderOutput.Add("}");

            File.WriteAllLines(foldername + @"\" + filePath.Split('.')[0] + "_merged.cpp", shaderOutput.ToArray());
            File.WriteAllText(foldername + @"\" + filePath.Split('.')[0] + "_header.h", HeaderFile);

            return true;
        }

        static string WriteMethods(ShaderMethod[] methods, ShaderField[] inoutFields, ShaderField[] uniforms)
        {
            string signatures = "";
            string output = "";

            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].isEntryPoint)
                    continue;

                for (int o = 0; o < uniforms.Length; o++)
                {
                    if (ContainsName(methods[i].contents, uniforms[o].name))
                        throw new Exception("XFDraw Parser does not support having uniforms inside other methods. Please copy them manually!");
                }

                for (int o = 0; o < inoutFields.Length; o++)
                {
                    if (ContainsName(methods[i].contents, inoutFields[o].name))
                        throw new Exception("XFDraw Parser does not support having fields inside other methods. Please copy them manually!");
                }

                if (ContainsName(methods[i].contents, "gl_FragCoord"))
                    throw new Exception("XFDraw Parser does not support having gl_FragCoord inside other methods. Please copy them manually!");


                string code = Regex.Replace(methods[i].contents, ";", ";\n\t");
                code = Regex.Replace(code, "{", "{\n\t");
                code = "\t" + Regex.Replace(code, "}", "}\n\t");

                signatures += methods[i].entryName + ";\n";
                output += methods[i].entryName + "{\n" + code + "\n}\n";
            }


            return signatures + "\n\n" + output;
        }

        static bool ContainsName(string input, string name)
        {
            int result = input.IndexOf(name);
            if (result == -1) return false;

            char first = result - 1 >= 0 ? input[result - 1] : (char)1;
            char secnd = result + name.Length < input.Length ? input[result + name.Length] : (char)1;

            if (first == (char)1 && secnd == (char)1) return true;

            if (char.IsLetterOrDigit(first) || char.IsLetterOrDigit(secnd))
                return false;

            return true;
        }

        static string WrapVariablePointer(string input, string name)
        {
            MatchCollection m = Regex.Matches(input, name);

            int offset = 0;
            for (int i = 0; i < m.Count; i++)
            {
                int result = m[i].Index + offset;
                if (result == -1) continue;

                char first = result - 1 >= 0 ? input[result - 1] : (char)1;
                char secnd = result + name.Length < input.Length ? input[result + name.Length] : (char)1;

                if (char.IsLetterOrDigit(first) || char.IsLetterOrDigit(secnd) || first == '*')
                    continue;

                input = input.Insert(result, "(*");
                input = input.Insert(result + name.Length + 2, ")");
                offset += 3;
            }

            return input;
        }

        static string RemoveBlockComments(string input)
        {

        Restart:
            int startIndex = -1;
            bool inComment = false;


            for (int i = 0; i < input.Length - 1; i++)
            {
                if (input[i] + "" + input[i + 1] == "/*")
                {
                    inComment = true;
                    startIndex = i;
                    i++;
                }

                if (input[i] + "" + input[i + 1] == "*/")
                {
                    if (!inComment) throw new Exception("Invalid Block Comment!");
                    inComment = false;

                    input = input.Remove(startIndex, (i + 1) - startIndex + 1);
                    goto Restart;
                }
            }

            if (inComment) throw new Exception("Invalid Block Comment!");

            return input;
        }

        static string[] RemoveComments(string[] str)
        {
            List<string> strlist = new List<string>();

            for (int i = 0; i < str.Length; i++)
            {
                if (str[i].Contains("//"))
                {
                    strlist.Add(str[i].Split(new[] { "//" }, StringSplitOptions.None)[0]);
                }
                else strlist.Add(str[i]);
            }

            return strlist.ToArray();
        }

        static string FlattenString(string[] lines)
        {
            string str = "";

            for (int i = 0; i < lines.Length; i++)
                str += lines[i].Trim();

            return str;
        }

        static ShaderParser Parse(string str)
        {
            str = Regex.Replace(str, @"\s+", " ");

            string mainMethod = "";

            List<ShaderField> shaderFields = new List<ShaderField>();
            List<ShaderStruct> shaderStructs = new List<ShaderStruct>();
            List<ShaderMethod> shaderMethods = new List<ShaderMethod>();
            bool hasEntry = false;

            string buildstr = "";
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '{')
                {
                    if (buildstr.StartsWith("void main()"))
                    {
                        if (hasEntry) throw new Exception("Two entry points detected!");

                        int frm = i + 1;
                        ReadMethod(i + 1, str, out i);
                        mainMethod = str.Substring(frm, i - frm - 1);

                        hasEntry = true;
                        buildstr = "";

                        if (i >= str.Length) break;
                    }
                    else if (buildstr.StartsWith("struct "))
                    {
                        int frm = i + 1;
                        ReadMethod(i + 1, str, out i);
                        shaderStructs.Add(new ShaderStruct(buildstr, str.Substring(frm, i - frm - 1)));
                        buildstr = "";

                        i++; //remove trailing: ;
                        if (i >= str.Length) break;
                    }
                    else if (buildstr.StartsWith("inline void main()"))
                    {
                        throw new Exception("Please do not manually inline the entry point!");
                    }
                    else
                    {
                        int frm = i + 1;
                        ReadMethod(i + 1, str, out i);
                        shaderMethods.Add(new ShaderMethod(buildstr, str.Substring(frm, i - frm - 1), false));

                        buildstr = "";

                        if (i >= str.Length) break;
                    }
                }

                if (str[i] == ';')
                {
                    //WARNING CAREFUL FOR VS layout location attrib!
                    shaderFields.Add(new ShaderField(buildstr));
                    buildstr = "";
                }
                else buildstr += str[i];
            }

            if (mainMethod == "") throw new Exception("No valid entry point found!");
            shaderMethods.Add(new ShaderMethod("void main()", mainMethod, true));


            //Validate methods with structs
            for (int i = 0; i < shaderFields.Count; i++)
            {
                if (shaderFields[i].dataType == DataType.Other)
                {
                    if (shaderFields[i].dataMode != DataMode.Uniform)
                        throw new Exception("Custom structs can only be uniform value!");

                    if (!shaderStructs.Any(shaderStruct => shaderStruct.structName == shaderFields[i].typeName))
                        throw new Exception("Could not find struct called \"" + shaderFields[i].typeName + "\" for \"" + shaderFields[i].name + "\"");
                }
            }

            ShaderField[] sf = shaderFields.ToArray();

            for (int i = 0; i < sf.Length; i++)
            {
                if (sf[i].dataType == DataType.Other)
                {
                    int count = 0;

                    for (int j = 0; j < shaderStructs.Count; j++)
                    {
                        if (sf[i].typeName == shaderStructs[j].structName)
                        {
                            sf[i].FieldSize = shaderStructs[j].Size;
                            sf[i].typeAlt = shaderStructs[j];
                            count++;
                        }
                    }
                    if (count == 0)
                        throw new Exception("Could not find a struct declaration of \"" + sf[i].typeName + "\"");
                    else if (count > 1)
                        throw new Exception("Multiple declarations of \"" + sf[i].typeName + "\" found!");
                }
            }

            ShaderField[] uni, field;
            int size = 0;
            PrepareFields(sf, out field, out uni, out size);

            return new ShaderParser(field, uni, shaderMethods.ToArray(), shaderStructs.ToArray());

        }

        static void ReadMethod(int currentIndex, string str, out int outindex)
        {
            int bracketCeption = 0;

            for (int i = currentIndex; i < str.Length; i++)
            {
                char s = str[i];

                if (str[i] == '{')
                    bracketCeption++;
                else if (str[i] == '}')
                    bracketCeption--;

                if (bracketCeption == -1)
                {
                    outindex = i + 1;
                    return;
                }
            }

            throw new Exception("Shader method is invalid!");
        }

        static void PrepareFields(ShaderField[] sFields, out ShaderField[] inout, out ShaderField[] uniform, out int Size)
        {
            List<ShaderField> uniforms = new List<ShaderField>();
            List<ShaderField> inouts = new List<ShaderField>();

            for (int i = 0; i < sFields.Length; i++)
            {
                if (sFields[i].dataMode == DataMode.Uniform)
                    uniforms.Add(sFields[i]);
                else inouts.Add(sFields[i]);
            }

            int offset = 0;
            for (int i = 0; i < uniforms.Count; i++)
            {
                uniforms[i].layoutPosition = offset;
                offset += uniforms[i].GetSize();
            }

            Size = offset;
            uniform = uniforms.ToArray();
            inout = inouts.ToArray();
        }

        static int TypeToSize(DataType dataType)
        {
            if (dataType == DataType.byte4) return 4;
            else if (dataType == DataType.fp32) return 4;
            else if (dataType == DataType.int2) return 8;
            else if (dataType == DataType.int32) return 4;
            else if (dataType == DataType.vec2) return 8;
            else if (dataType == DataType.vec3) return 12;
            else throw new Exception("not implemented yet!");
        }

        static string TypeToString(DataType dataType)
        {
            if (dataType == DataType.byte4) return "byte4";
            else if (dataType == DataType.fp32) return "float";
            else if (dataType == DataType.int2) return "int2";
            else if (dataType == DataType.int32) return "int";
            else if (dataType == DataType.vec2) return "vec2";
            else if (dataType == DataType.vec3) return "vec3";
            else if (dataType == DataType.mat4) return "mat4";
            else if (dataType == DataType.mat4) return "mat3";
            else throw new Exception("not implemented yet!");
        }

    }

    internal class ShaderField
    {
        internal string name;
        internal DataType dataType;
        internal DataMode dataMode;

        internal string typeName;
        internal ShaderStruct typeAlt = null;

        internal int layoutPosition;
        internal int FieldSize;

        internal int layoutValueGL = -1;

        public ShaderField(string inputString)
        {
            if (inputString.StartsWith("layout"))
            {
                int pos = inputString.IndexOf(")");
                if (pos == 0) throw new Exception("An error occured (28105)");
                if (!int.TryParse(inputString[pos - 1].ToString(), out layoutValueGL))
                    throw new Exception("Failed to read layout value!");

                inputString = inputString.Substring(pos + 1, inputString.Length - pos - 1).Trim();

            }

            string[] str = inputString.Trim().Split(' ');

            if (str.Length != 3) throw new Exception("Unknown Data!");

            name = str[2];
            dataType = toDataType(str[1]);
            dataMode = toDataMode(str[0]);
            layoutPosition = -1;
            FieldSize = dataType != DataType.Other ? -1 : -1;
            typeName = dataType == DataType.Other ? str[1] : str[1];
        }

        internal static DataType toDataType(string input)
        {
            if (input == "byte4") return DataType.byte4;
            else if (input == "float") return DataType.fp32;
            else if (input == "int2") return DataType.int2;
            else if (input == "int") return DataType.int32;
            else if (input == "vec2") return DataType.vec2;
            else if (input == "vec3") return DataType.vec3;
            else if (input == "mat3") return DataType.mat3;
            else if (input == "mat4") return DataType.mat4;

            else return DataType.Other;
        }

        internal static DataMode toDataMode(string input)
        {
            if (input == "in") return DataMode.In;
            else if (input == "out") return DataMode.Out;
            else if (input == "uniform") return DataMode.Uniform;
            else if (input == "const") throw new Exception("Unknown data mode!");
            else throw new Exception("Unknown data mode: " + input);
        }

        internal int GetSize()
        {
            if (dataType == DataType.byte4) return 4;
            else if (dataType == DataType.fp32) return 4;
            else if (dataType == DataType.int2) return 8;
            else if (dataType == DataType.int32) return 4;
            else if (dataType == DataType.vec2) return 8;
            else if (dataType == DataType.vec3) return 12;
            else if (dataType == DataType.mat4) return 64;
            else if (dataType == DataType.mat3) return 36;
            else if (dataType == DataType.Other && FieldSize != -1) return FieldSize;
            else throw new Exception("An unknown error occured (00245)");
        }

        internal void SetLayoutOffset(int value)
        {
            layoutPosition = value;
        }
    }

    internal class ShaderMethod
    {
        internal string entryName;
        internal string contents;
        internal bool isEntryPoint;

        public ShaderMethod(string start, string input, bool isEntry)
        {
            entryName = start;
            contents = input;
            isEntryPoint = isEntry;
        }
    }

    internal class ShaderStruct
    {
        internal string structName;
        internal StructField[] structFields;
        internal int Size;
        internal string data;

        public ShaderStruct(string start, string str)
        {
            List<StructField> sFields = new List<StructField>();

            string buildstr = "";
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '{') throw new Exception("Structs cannot contain any constructors or brackets!");

                if (str[i] == ';')
                {
                    sFields.Add(new StructField(buildstr));
                    buildstr = "";
                }
                else buildstr += str[i];
            }

            int offset = 0;
            for (int i = 0; i < sFields.Count; i++)
            {
                sFields[i].layoutValue = offset;
                offset += sFields[i].GetSize();
            }

            Size = offset;

            data = str;
            structFields = sFields.ToArray();
            structName = start.Trim().Split()[1];
        }

        public unsafe void SetValue(string uniformName, int offset, byte[] uniformBytesFS, object value)
        {
            int setCount = 0;

            for (int i = 0; i < structFields.Length; i++)
            {
                if (structFields[i].name == uniformName)
                {
                    int mSize = Marshal.SizeOf(value);
                    if (structFields[i].GetSize() != mSize)
                        throw new Exception("\"" + uniformName + "\" is not the same size as the value!");

                    GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
                    byte* ptr = (byte*)handle.AddrOfPinnedObject();

                    if (structFields[i].layoutValue == -1)
                        throw new Exception("An error occured (1924)");

                    for (int n = 0; n < mSize; n++)
                        uniformBytesFS[structFields[i].layoutValue + offset + n] = ptr[n];

                    handle.Free();

                    setCount++;
                }
            }

            if (setCount == 0)
                throw new Exception("Uniform value \"" + uniformName + "\" was not found in the shader!");
            else if (setCount >= 2)
                throw new Exception("Uniform value \"" + uniformName + "\" was found multiple times in the shader!");
        }
    }

    internal class StructField
    {
        internal string name;
        internal DataType dataType;

        internal int layoutValue;
        internal int FieldSize;

        public StructField(string inputString)
        {
            string[] str = inputString.Trim().Split(' ');

            if (str.Length != 2) throw new Exception("Unknown Data!");

            name = str[1];
            dataType = ShaderField.toDataType(str[0]);

            if (dataType == DataType.Other)
                throw new Exception("Unknown or Struct Types are not allowed inside a existing struct!");

            layoutValue = -1;
            FieldSize = (int)dataType;
        }

        internal int GetSize()
        {
            if (dataType == DataType.byte4) return 4;
            else if (dataType == DataType.fp32) return 4;
            else if (dataType == DataType.int2) return 8;
            else if (dataType == DataType.int32) return 4;
            else if (dataType == DataType.vec2) return 8;
            else if (dataType == DataType.vec3) return 12;
            else if (dataType == DataType.mat4) return 64;
            else if (dataType == DataType.mat3) return 36;
            else
                throw new Exception("An error occured (00145)");
        }
    }

    enum DataType
    {
        vec3,// = 4 * 3,
        vec2,// = 4 * 2,
        fp32,// = 4,
        int32,// = 4,
        int2,// = 4 * 2,
        byte4,// = 4,
        mat4,
        mat3,
        Other
    }

    enum DataMode
    {
        In,
        Out,
        Uniform
    }

    public enum CompileOption
    {
        None,
        AddInlineAll,
        ManualInlineEntry,
        ForceRecompile,
        EnableSIMD,
        UseOMP,
        UsePPL,
        UseFor
    }
}
