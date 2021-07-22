using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using xfcore.Shaders;

namespace xfcore.Shaders.Builder
{
    public class ShaderCompile
    {
        public static string COMPILER_LOCATION = @"C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\bin\";
        public static string COMPILER_NAME = "cl.exe";
        public static string COMMAND_LINE = "/openmp /nologo /GS /GL /Zc:forScope /Oi /MD -ffast-math /O2 /fp:fast -Ofast /Oy /Og /Ox /Ot ";

        ShaderField[] sFieldsVS;
        ShaderMethod[] sMethodsVS;
        ShaderStruct[] sStructsVS;

        ShaderField[] sFieldsFS;
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
                sMethodsVS = VS.shaderMethods;
                sStructsVS = VS.shaderStructs;
            }

            sFieldsFS = FS.shaderFields;
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

            for (int i = 0; i < sFieldsFS.Length; i++)
                if (sFieldsFS[i].dataMode == DataMode.Uniform)
                    uniforms.Add(sFieldsFS[i]);

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

            for (int i = 0; i < sFieldsVS.Length; i++)
                if (sFieldsVS[i].dataMode == DataMode.Uniform)
                    uniforms.Add(sFieldsVS[i]);

            int offset = 0;
            for (int i = 0; i < uniforms.Count; i++)
            {
                uniforms[i].SetLayoutOffset(offset);
                offset += uniforms[i].GetSize();
            }

            Size = offset;
            return uniforms.ToArray();
        }

        internal ShaderField[] PrepareFieldFS()
        {
            List<ShaderField> fields = new List<ShaderField>();

            for (int i = 0; i < sFieldsFS.Length; i++)
                if (sFieldsFS[i].dataMode != DataMode.Uniform)
                    fields.Add(sFieldsFS[i]);

            return fields.ToArray();
        }


    }

    public class ShaderParser
    {
        internal ShaderField[] shaderFields;
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

        ShaderParser(ShaderField[] f, ShaderMethod[] m, ShaderStruct[] s)
        {
            shaderFields = f;
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

            string namePath = "";

            WriteShaders(vertexShader, fragmentShader);
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


        static void WriteShaders(string filePath1, string filePath2)
        {
            string ext = filePath1.Split('.')[0] + "_" + filePath2.Split('.')[0];

            string foldername = "_temp_" + ext;
            //realPath = foldername;

            if (!Directory.Exists(foldername))
                Directory.CreateDirectory(foldername);

            bool prevFile = false;

            if (File.Exists(foldername + @"\" + filePath1))
            {
                if (File.ReadLines(filePath1).SequenceEqual(File.ReadLines(foldername + @"\" + filePath1)))
                    prevFile = true;
                else
                    File.Delete(foldername + @"\" + filePath1);
            }

            if (File.Exists(foldername + @"\" + filePath2))
            {
                if (File.ReadLines(filePath2).SequenceEqual(File.ReadLines(foldername + @"\" + filePath2)) && prevFile)
                    return;

                File.Delete(foldername + @"\" + filePath2);
            }


            File.Copy(filePath1, foldername + @"\" + filePath1);
            File.Copy(filePath2, foldername + @"\" + filePath2);



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

            string execSignPtr = ""; //inline void shaderMethod(etc etc)
            string execSignUni = ""; //inline void shaderMethod(etc etc)

            if (!data.shaderMethods[data.shaderMethods.Length - 1].isEntryPoint)
                throw new Exception("No void main() shader entry point detected!");

            string mainCode = data.shaderMethods[data.shaderMethods.Length - 1].contents;

            
            int maxsize = 0;
            ShaderField[] uniforms, inoutFields;
            PrepareFields(data.shaderFields, out inoutFields, out uniforms, out maxsize);

            string methods = WriteMethods(data.shaderMethods, inoutFields, uniforms);


            for (int i = 0; i < uniforms.Length; i++)
            {
                if (uniforms[i].dataType == DataType.vec2)
                {
                    uFields += indt1 + "vec2 " + "uniform_" + i + ";\n";
                    uFields += indt1 + "fcpy((char*)(&uniform_" + i + "), (char*)UniformPointer + " + uniforms[i].layoutPosition + ", 8);\n";

                    methdSignUnifm += "uniform_" + i + ", ";
                    execSignUni += "vec2 " + uniforms[i].name + ", ";
                }
                else if (uniforms[i].dataType == DataType.int32)
                {
                    uFields += indt1 + "int " + "uniform_" + i + ";\n";
                    uFields += indt1 + "fcpy((char*)(&uniform_" + i + "), (char*)UniformPointer + " + uniforms[i].layoutPosition + ", 4);\n";

                    methdSignUnifm += "uniform_" + i + ", ";
                    execSignUni += "int " + uniforms[i].name + ", ";
                }
                else throw new NotImplementedException();
            }

            for (int i = 0; i < inoutFields.Length; i++)
            {
                if (inoutFields[i].dataType == DataType.byte4 || inoutFields[i].dataType == DataType.int32)
                {
                    ptrs.Add(indt + "int* " + "ptr_" + i + " = (int*)(ptrPtrs[" + i + "] + wPos * 4);\n");
                    ptrsIncr += ", ++ptr_" + i;
                    methdSign += "ptr_" + i + ", ";
                    execSignPtr += (inoutFields[i].dataType == DataType.byte4 ? "byte4* " : "int* ") + inoutFields[i].name + ", ";
                }
                else if (inoutFields[i].dataType == DataType.fp32)
                {
                    ptrs.Add(indt + "float* " + "ptr_" + i + " = (float*)(ptrPtrs[" + i + "] + wPos * 4);\n");
                    ptrsIncr += ", ++ptr_" + i;
                    methdSign += "ptr_" + i + ", ";
                    execSignPtr += "float* " + inoutFields[i].name + ", ";
                }
                else throw new NotImplementedException("not yet!");
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

            shaderOutput.Add("#include \"" + filePath.Split('.')[0] + "_header.h" + "\"\n");

            shaderOutput.Add(methods);

            shaderOutput.Add("inline void shaderMethod(" + methodSignExec + "){");
            shaderOutput.Add(mainCode + "\n}");

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

            return new ShaderParser(shaderFields.ToArray(), shaderMethods.ToArray(), shaderStructs.ToArray());

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



    }

    internal class ShaderField
    {
        internal string name;
        internal DataType dataType;
        internal DataMode dataMode;

        internal string typeName;

        internal int layoutPosition;
        internal int FieldSize;

        public ShaderField(string inputString)
        {
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
            else throw new Exception("not implemented yet!");
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

        public ShaderStruct(string start, string str)
        {
            List<StructField> sFields = new List<StructField>();

            string buildstr = "";
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == ';')
                {
                    sFields.Add(new StructField(buildstr));
                    buildstr = "";
                }
                else buildstr += str[i];
            }

            structFields = sFields.ToArray();
            structName = start.Trim().Split()[1];
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

    }

    enum DataType
    {
        vec3,// = 4 * 3,
        vec2,// = 4 * 2,
        fp32,// = 4,
        int32,// = 4,
        int2,// = 4 * 2,
        byte4,// = 4,
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
        ForceInline,
        ForceAggressiveInline,
        ForceRecompile,
        ParseOnly
    }
}
