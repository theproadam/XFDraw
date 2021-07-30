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
        public static string COMMAND_LINE = "/openmp /nologo /GS /GL /Oi /MD /O2 /fp:fast -Ofast /Oy /Ox /Ot"; //GL hang?

        internal ShaderField[] sFieldsInVS;
        internal ShaderField[] sFieldsOutVS;
        internal ShaderField[] sUniformsVS;
        internal ShaderMethod[] sMethodsVS;
        internal ShaderStruct[] sStructsVS;

        internal ShaderField[] sFieldsInFS;
        internal ShaderField[] sFieldsOutFS;
        internal ShaderField[] sUniformsFS;
        internal ShaderMethod[] sMethodsFS;
        internal ShaderStruct[] sStructsFS;

        bool skipCompile = false;
        bool isScreenSpace = false;
        string shaderName;

        internal int readStride;
        internal int inteStride;

        internal ShaderCompile(ShaderParser VS, ShaderParser FS, bool isScreen, string sName, bool skipcompile)
        {
            if (!isScreen)
            {
                sFieldsInVS = VS.shaderFieldIns;
                sFieldsOutVS = VS.shaderFieldOuts;
                sUniformsVS = VS.shaderUniforms;
                sMethodsVS = VS.shaderMethods;
                sStructsVS = VS.shaderStructs;

                readStride = VS.readStride / 4;
                inteStride = VS.internalStride / 4;
            }

            sFieldsInFS = FS.shaderFieldIns;
            sFieldsOutFS = FS.shaderFieldOuts;
            sUniformsFS = FS.shaderUniforms;
            sMethodsFS = FS.shaderMethods;
            sStructsFS = FS.shaderStructs;

           

            isScreenSpace = isScreen;
            shaderName = sName;

            skipCompile = skipcompile;
        }

        public bool Compile(out Shader compiledShader)
        {
            compiledShader = null;

            string folder = "_temp_" + shaderName;

            if (skipCompile)
            {
                compiledShader = new Shader(folder + @"\xf" + shaderName + ".dll", this);
                return true;
            }

            string cppFileSource = "xf" + shaderName + ".cpp";
            string headerFile = "xfcore.h";

            if (!File.Exists(folder + @"\" + cppFileSource) || !File.Exists(folder + @"\" + headerFile))
                throw new FileNotFoundException("Missing File!");

            string path = System.AppDomain.CurrentDomain.BaseDirectory;
            string tempPath = path + folder + "\\";// +"_temp_" + cpp;
            string outdir = tempPath + "xf" + shaderName + ".dll";

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


            compiledShader = new Shader(outdir, this);

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

        internal bool GetScreenSpace()
        {
            return isScreenSpace;
        }

    }

    [Serializable]
    public partial class ShaderParser
    {
        internal ShaderField[] shaderFieldOuts;
        internal ShaderField[] shaderFieldIns;
        internal ShaderField[] shaderUniforms;
        internal ShaderMethod[] shaderMethods;
        internal ShaderStruct[] shaderStructs;

        internal int internalStride;
        internal int readStride;

        static string entry = "extern \"C\" __declspec(dllexport) void ";
        internal static int Shader_Version;

        ShaderParser(ShaderField[] f, ShaderField[] u, ShaderMethod[] m, ShaderStruct[] s)
        {
            PrepVSData(f, out shaderFieldIns, out shaderFieldOuts, out readStride, out internalStride);

            shaderUniforms = u;
            shaderMethods = m;
            shaderStructs = s;
        }

        public static ShaderCompile Parse(string vertexShader, string fragmentShader, string outputName, params CompileOption[] compileOptions)
        {
            if (outputName.Contains('.'))
                throw new Exception("OutputName will have its own extension. Please do not add one.");

            if (vertexShader.Contains('.') && vertexShader.Split('.')[0] == outputName)
                throw new Exception("outputName must be unique when compared to vertexShader!");

            if (fragmentShader.Contains('.') && fragmentShader.Split('.')[0] == outputName)
                throw new Exception("outputName must be unique when compared to fragmentShader!");


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
            
            bool skipcompile = !WriteShaders(vertexShader, fragmentShader, outputName, vsModule, fsModule, compileOptions);
            return new ShaderCompile(vsModule, fsModule, false, outputName, skipcompile);
        }

        public static ShaderCompile Parse(string screenspaceShader, string outputName, params CompileOption[] compileOptions)
        {
            if (outputName.Contains('.'))
                throw new Exception("OutputName will have its own extension. Please do not add one.");

            if (screenspaceShader.Contains('.') && screenspaceShader.Split('.')[0] == outputName)
                throw new Exception("outputName must be unique when compared to screenspaceShader!");

            if (!File.Exists(screenspaceShader))
                throw new FileNotFoundException();

            string[] analyzeS = File.ReadAllLines(screenspaceShader);
            string SReady = PrepareInput(analyzeS);
            ShaderParser sModule = Parse(SReady);

            bool skipcompile = !WriteShader(screenspaceShader, outputName, sModule, compileOptions);
            return new ShaderCompile(null, sModule, true, outputName, skipcompile);
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

            for (int i = 0; i < vs.shaderFieldOuts.Length; i++)
            {
                int count = 0;
                fFieldC = 0;

                for (int j = 0; j < fs.shaderFieldIns.Length; j++)
                {
                    fFieldC++;
                    if (vs.shaderFieldOuts[i].name == fs.shaderFieldIns[j].name)
                    {
                        count++;
                        if (vs.shaderFieldOuts[i].GetSize() != fs.shaderFieldIns[j].GetSize())
                            throw new Exception("The vs OUT \"" + vs.shaderFieldOuts[i].name + "\" is not the same size as the FS in!");
                    }
                }

                vFieldC++;
                if (count == 0) throw new Exception("The vs OUT \"" + vs.shaderFieldOuts[i].name + "\" does not exist in the fs!");
                if (count > 1) throw new Exception("The vs OUT \"" + vs.shaderFieldOuts[i].name + "\" exists multiple times in the fs!");
            }

            if (vFieldC != fFieldC) throw new Exception("There seems to me a mismatch in the attribute count between the vs and fs!");

            ShaderField[] reOrgData = new ShaderField[fs.shaderFieldIns.Length];

            if (vs.shaderFieldOuts.Length != fs.shaderFieldIns.Length)
                throw new Exception("A vs OUT and fs IN mismatched occured, but the previous test passed. Please report This!");

            //technically this loop should have been integrated earlier
            for (int i = 0; i < vs.shaderFieldOuts.Length; i++)
            {
                for (int j = 0; j < fs.shaderFieldIns.Length; j++)
                {
                    if (vs.shaderFieldOuts[i].name == fs.shaderFieldIns[j].name)
                    {
                        reOrgData[i] = fs.shaderFieldIns[j];
                    }
                }
            }

            fs.shaderFieldIns = reOrgData;

            int offset = 0;
            for (int i = 0; i < fs.shaderFieldIns.Length; i++)
            {
                fs.shaderFieldIns[i].layoutPosition = offset;
                offset += fs.shaderFieldIns[i].GetSize();
            }

        }

        static void CheckForNonFloats(ShaderParser vs)
        {
            for (int i = 0; i < vs.shaderFieldOuts.Length; i++)
            {
                DataType d = vs.shaderFieldOuts[i].dataType;

                if (!(d == DataType.vec2 || d == DataType.vec3))
                    throw new Exception("The vertex shader can only work with floating point types!");
            }

            for (int i = 0; i < vs.shaderFieldIns.Length; i++)
            {
                DataType d = vs.shaderFieldIns[i].dataType;

                if (!(d == DataType.vec2 || d == DataType.vec3))
                    throw new Exception("The vertex shader in's only work with Vector3 types! (vec3)");
            }

        }

        static bool WriteShaders(string filePath1, string filePath2, string outputName, ShaderParser vs, ShaderParser fs, CompileOption[] cOps)
        {
            string ext = outputName;
            string wPath = ext;

            ShaderField[] vsIn = vs.shaderFieldIns, vsOut = vs.shaderFieldOuts;
            ShaderField[] fsIn = fs.shaderFieldIns, fsOut = fs.shaderFieldOuts;
            
            int intS = vs.internalStride / 4;
            int readS = vs.readStride / 4;
            intS += 3;

            bool forceC = cOps.Contains(CompileOption.ForceRecompile);
            bool hasSerial = !cOps.Contains(CompileOption.DoNotSerialize);

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

            if (!File.Exists(foldername + @"\xf" + wPath + ".dll"))
                prevFile = false;

            if (File.Exists(foldername + @"\" + filePath2))
            {
                if (File.ReadLines(filePath2).SequenceEqual(File.ReadLines(foldername + @"\" + filePath2)) && prevFile)
                    return false;

                File.Delete(foldername + @"\" + filePath2);
            }

            if (File.Exists(foldername + @"\" + filePath1))
            {
                File.Delete(foldername + @"\" + filePath1);
            }

            File.Copy(filePath1, foldername + @"\" + filePath1);
            File.Copy(filePath2, foldername + @"\" + filePath2);

            #endregion

            if (!ContainsName(vs.shaderMethods[vs.shaderMethods.Length - 1].contents, "gl_Position"))
                throw new Exception("The vertex shader needs the gl_Position vector3 set!");

            if (ContainsName(vs.shaderMethods[vs.shaderMethods.Length - 1].contents, "FSExec")) throw new Exception("FSExec is a reserved name!");
            if (ContainsName(vs.shaderMethods[vs.shaderMethods.Length - 1].contents, "VSExec")) throw new Exception("VSExec is a reserved name!");
            if (ContainsName(fs.shaderMethods[fs.shaderMethods.Length - 1].contents, "VSExec")) throw new Exception("VSExec is a reserved name!");
            if (ContainsName(fs.shaderMethods[fs.shaderMethods.Length - 1].contents, "FSExec")) throw new Exception("FSExec is a reserved name!");

            string sign, exec;
            WriteVSSign(vs, vsIn, vsOut, out sign, out exec);

            string sign1, exec1, ptrs1, ptrIncr;
            WriteFSSign(fs, fsIn, fsOut, out sign1, out exec1, out ptrs1, out ptrIncr);

            string shaderCode = "", entryCode = "";

            entryCode += "//Autogenerated by XFParser\n\n";
            entryCode += "#include <malloc.h>\n#include \"xfcore.cpp\"\n#include \"xfcore.h\"\n#include <math.h>\n#include <ppl.h>\nusing namespace Concurrency;\n";
            if (hasSerial) entryCode += "#include \"xfconfig.cpp\"\n";

            entryCode += "\n#define RETURN_VALUE\n#define RtlZeroMemory frtlzeromem\n";

            string structDeclrs = "";

            for (int i = 0; i < vs.shaderUniforms.Length; i++)
            {
                if (vs.shaderUniforms[i].dataType == DataType.Other)
                    structDeclrs += "\nstruct " + vs.shaderUniforms[i].typeName + " {\n" + vs.shaderUniforms[i].typeAlt.data + "\n};\n";
            }

            for (int i = 0; i < fs.shaderUniforms.Length; i++)
            {
                if (fs.shaderUniforms[i].dataType == DataType.Other)
                    structDeclrs += "\nstruct " + fs.shaderUniforms[i].typeName + " {\n" + fs.shaderUniforms[i].typeAlt.data + "\n};\n";
            }

            entryCode += structDeclrs + "\n";

            entryCode += WriteMethods(vs.shaderMethods, vsOut, vsIn, vs.shaderUniforms);

            entryCode += sign + "{\n" + WriteExecMethod(vs, true) + "\n}\n\n";
            entryCode += sign1 + "{\n" + WriteExecMethod(fs, false) + "\n}\n\n";


            shaderCode += "void MethodExec(int index, float* p, float* dptr, char* uData1, char* uData2, unsigned char** ptrPtrs, GLData projData, int facecull, int isWire){\n";
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

            shaderCode = Regex.Replace(shaderCode, "\n", "\n\t");

            

            //Write ClippingCode ->
            shaderCode += ClippingCode + "\n";
            shaderCode += "\t" + Transforms + "\n";
            shaderCode += "\t" + FaceCulling;
            shaderCode += "\n\t" + ScanLineStart;
            shaderCode += "\n\n\t\t\t" + "int wPos = renderWidth * i;\n" + "\t\t\t";
            shaderCode += Regex.Replace(ptrs1, "\n", "\n\t") + "\n";

            //Depth ->
            shaderCode += "\t\t\t" + "Z_fptr = dptr + i * renderWidth;\n\t\t\tzBegin = slopeZ * (float)FromX + bZ;\n\n";

            //DrawScanline
            shaderCode += "\t\t\t" + "for (int o = FromX; o <= ToX; ++o " + "" + "){\n"; //ptrsIncrWarn

            shaderCode += "\t\t\t\t" + @"float depth = (1.0f / zBegin);
				s = projData.farZ - depth;
				zBegin += slopeZ;

				if (Z_fptr[o] > s) continue;
				Z_fptr[o] = s;

				if (usingZ) for (int z = 0; z < stride - 3; z++) attribs[z] = (y_Mxb[z] * depth + y_mxB[z]);
				else for (int z = 0; z < stride - 3; z++) attribs[z] = (y_Mxb[z] * (float)o + y_mxB[z]);";

            shaderCode += "\n\n\t\t\t\t" + exec1 + "\n";
            shaderCode += "\t\t\t}\n\t\t}\n\t}\n}";

            shaderCode = entryCode + shaderCode;
            shaderCode += "\n\n" + (cOps.Contains(CompileOption.UseFor) ? NotParallelCode : ParallelCode);


            if (hasSerial)
                File.WriteAllText(foldername + @"\xfconfig.cpp", WriteSerializationBuffer(vs, fs));
            else
            {
                string str1 = entry + "ReadyShader(long* vs_parse_size, long* fs_parse_size, long* compiled_version)\n{\n";

                str1 += "\t*vs_parse_size = " + 0 + ";\n";
                str1 += "\t*fs_parse_size = " + 0 + ";\n";
                str1 += "\t*compiled_version = 0;\n}\n\n";

                shaderCode += str1;
            }


            File.WriteAllText(foldername + @"\" + "xf" + wPath + ".cpp", shaderCode);
            File.WriteAllText(foldername + @"\" + "xfcore.h", HeaderFile);
            File.WriteAllText(foldername + @"\xfcore.cpp", ClipCodeSource);
         
            return true;
        }

        static bool WriteShader(string filePath, string outputName, ShaderParser data, CompileOption[] cOps)
        {
            string foldername = "_temp_" + outputName;
            string writtenPath = outputName;

            if (!Directory.Exists(foldername))
                Directory.CreateDirectory(foldername);

            if (File.Exists(foldername + @"\" + filePath))
            {
                if (File.ReadLines(filePath).SequenceEqual(File.ReadLines(foldername + @"\" + filePath)) && !cOps.Contains(CompileOption.ForceRecompile))
                    if (File.Exists(foldername + @"\xf" + writtenPath + ".dll"))
                        return false;        

                File.Delete(foldername + @"\" + filePath);
            }

            File.Copy(filePath, foldername + @"\" + filePath);

            bool hasSerial = !cOps.Contains(CompileOption.DoNotSerialize);

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

            ShaderField[] inoutFields = data.shaderFieldIns.Concat(data.shaderFieldOuts).ToArray();

            ShaderField[] uniforms = data.shaderUniforms;
            string methods = WriteMethods(data.shaderMethods, data.shaderFieldOuts, data.shaderFieldIns, uniforms);

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
            shaderOutput.Add("#include \"" + "xfcore.h" + "\"\n");

            if (hasSerial) shaderOutput.Add("#include \"xfconfig.cpp\"\n");

            if (structDeclrs != "") shaderOutput.Add(structDeclrs);
            if (methods != "") shaderOutput.Add(methods);  

            shaderOutput.Add("inline void shaderMethod(" + methodSignExec + "){\n" + mainCode + "\n}");

            //Add Base Part
           
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

            if (hasSerial)
                File.WriteAllText(foldername + @"\xfconfig.cpp", WriteSerializationBuffer(null, data));
            else
            {
                string str1 = entry + "ReadyShader(long* vs_parse_size, long* fs_parse_size, long* compiled_version)\n{\n";

                str1 += "\t*vs_parse_size = " + 0 + ";\n";
                str1 += "\t*fs_parse_size = " + 0 + ";\n";
                str1 += "\t*compiled_version = 0;\n}\n\n";

                shaderOutput.Add("\n\n\n" + str1);
            }

            File.WriteAllLines(foldername + @"\xf" + outputName + ".cpp", shaderOutput.ToArray());
            File.WriteAllText(foldername + @"\" + "xfcore.h", HeaderFile);

            return true;
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
                    if (buildstr != "")
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
            else if (dataType == DataType.sampler2D) return 20;
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
            else if (dataType == DataType.mat3) return "mat3";
            else if (dataType == DataType.sampler2D) return "sampler2D";
            else throw new Exception("not implemented yet!");
        }

        static string WriteSerializationBuffer(ShaderParser VS, ShaderParser FS)
        {          
            string vs_buffer = "const unsigned char vs_serial_buffer[] = ";
            string fs_buffer = "const unsigned char fs_serial_buffer[] = ";

            int vSize = 0;
            int fSize = 0;

            if (VS != null)
            {
                byte[] VS_BUF = Serializer.Serialize(VS);
                string buf = "{";

                for (int i = 0; i < VS_BUF.Length; i++)
                    buf += VS_BUF[i] + ",";

                buf = buf.Substring(0, buf.Length - 1) + " };\n\n";
                vs_buffer += buf;

                vSize = VS_BUF.Length;
            }
            else
                vs_buffer += "{0};\n\n";

            if (true)
            {
                byte[] FS_BUF = Serializer.Serialize(FS);

                string buf = "{";

                for (int i = 0; i < FS_BUF.Length; i++)
                    buf += FS_BUF[i] + ",";

                buf = buf.Substring(0, buf.Length - 1) + " };\n\n";
                fs_buffer += buf;

                fSize = FS_BUF.Length;
            }

            string warning = "//This autogenerated file contains serialization data that XFDraw uses for reflection.\n";
            warning += "//DO NOT MODIFY THE CONTENTS OF THIS FILE. DOING SO WILL SEGFAULT THE SHADER.\n\n";

            string str =  entry + "ReadyShader(long* vs_parse_size, long* fs_parse_size, long* compiled_version)\n{\n";

            str += "\t*vs_parse_size = " + vSize + ";\n";
            str += "\t*fs_parse_size = " + fSize + ";\n";
            str += "\t*compiled_version = " + Shader_Version + ";\n}\n\n";

            str += entry + @"LoadData(unsigned char* vsPtr, unsigned char* fsPtr)
{
	for (int i = 0; i < " + vSize + @"; i++)
	{
		vsPtr[i] = vs_serial_buffer[i];
	}

	for (int i = 0; i < " + fSize + @"; i++)
	{
		fsPtr[i] = fs_serial_buffer[i];
	}
}";

            return warning + vs_buffer + fs_buffer + str;
        }
    }

    public partial class ShaderParser
    {
        static string HeaderFile
        {
            get
            {
                return @"#include <cstdint>
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
	long* TEXTURE_ADDR;
    int mode;
    int mode_color;
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

extern " + "\"C\"" + @" __declspec(dllexport) int32_t CheckSize()
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

byte4 textureBILINEAR(sampler2D inputTexture, vec2 coord, bool sampleAlpha = false)
{
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

byte4 textureNEAREST(sampler2D inputTexture, int2 coord)
{
	if (TEXTURE_WRAP_MODE == 0)
	{
		if (coord.X < 0) coord.X = 0;
		if (coord.Y < 0) coord.Y = 0;
		if (coord.X >= inputTexture.width) coord.X = inputTexture.width - 1;
		if (coord.Y >= inputTexture.height) coord.Y = inputTexture.height - 1;
	}
	else if (TEXTURE_WRAP_MODE == 1)
	{
		if (coord.X < 0) coord.X = coord.X % inputTexture.width + inputTexture.width;
		if (coord.Y < 0) coord.Y = coord.Y % inputTexture.height + inputTexture.height;

		if (coord.X >= inputTexture.width) coord.X = coord.X % inputTexture.width;
		if (coord.Y >= inputTexture.height) coord.Y = coord.Y % inputTexture.height;
	}
	else
	{
		if (coord.X < 0 || coord.Y < 0 || coord.X >= inputTexture.width || coord.Y >= inputTexture.height) return TEXTURE_CLAMP_BORDER_COLOR;
	}

	return *((byte4*)(inputTexture.TEXTURE_ADDR + inputTexture.width * coord.Y + coord.X));
}


";
            }
        }

        static string IncludeFileVSFS
        {
            get { return @""; }
        }

        static string ClippingCode
        {
            get
            {
                return @"#pragma region NearPlaneCFG

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
#pragma endregion";
            }
        }

        static string Transforms
        {
            get
            {
                return @"int renderWidth = projData.renderWidth, renderHeight = projData.renderHeight;

	float yMaxValue = 0;
	float yMinValue = renderHeight - 1;

	//temp variables ->
	float fwi = 1.0f / projData.fw;
	float fhi = 1.0f / projData.fh;
	float ox = projData.ox, oy = projData.oy;
	
	//XYZ-> XY Transforms
	
    float mMinOne = (1.0f - projData.matrixlerpv);

	if (projData.matrixlerpv == 0)
		for (int im = 0; im < BUFFER_SIZE; im++)
		{
			VERTEX_DATA[im * stride + 0] = roundf(projData.rw + (VERTEX_DATA[im * stride + 0] / VERTEX_DATA[im * stride + 2]) * projData.fw);
			VERTEX_DATA[im * stride + 1] = roundf(projData.rh + (VERTEX_DATA[im * stride + 1] / VERTEX_DATA[im * stride + 2]) * projData.fh);
			VERTEX_DATA[im * stride + 2] = 1.0f / (VERTEX_DATA[im * stride + 2]);

			if (VERTEX_DATA[im * stride + 1] > yMaxValue) yMaxValue = VERTEX_DATA[im * stride + 1];
			if (VERTEX_DATA[im * stride + 1] < yMinValue) yMinValue = VERTEX_DATA[im * stride + 1];
		}
	else if (projData.matrixlerpv == 1)
		for (int im = 0; im < BUFFER_SIZE; im++)
		{
			VERTEX_DATA[im * stride + 0] = roundf(projData.rw + VERTEX_DATA[im * stride + 0] * projData.iox);
			VERTEX_DATA[im * stride + 1] = roundf(projData.rh + VERTEX_DATA[im * stride + 1] * projData.ioy);

			if (VERTEX_DATA[im * stride + 1] > yMaxValue) yMaxValue = VERTEX_DATA[im * stride + 1];
			if (VERTEX_DATA[im * stride + 1] < yMinValue) yMinValue = VERTEX_DATA[im * stride + 1];
		}
	else
		for (int im = 0; im < BUFFER_SIZE; im++)
		{
			VERTEX_DATA[im * stride + 0] = roundf(projData.rw + VERTEX_DATA[im * stride + 0] / ((VERTEX_DATA[im * stride + 2] * fwi - ox) * mMinOne + ox));
			VERTEX_DATA[im * stride + 1] = roundf(projData.rh + VERTEX_DATA[im * stride + 1] / ((VERTEX_DATA[im * stride + 2] * fhi - oy) * mMinOne + oy));
			VERTEX_DATA[im * stride + 2] = 1.0f / (VERTEX_DATA[im * stride + 2] + projData.oValue);


			if (VERTEX_DATA[im * stride + 1] > yMaxValue) yMaxValue = VERTEX_DATA[im * stride + 1];
			if (VERTEX_DATA[im * stride + 1] < yMinValue) yMinValue = VERTEX_DATA[im * stride + 1];
		}
";
            }
        }

        static string FaceCulling
        {
            get
            {
                return @"if (facecull == 1 || facecull == 2)
	{
		float A = BACKFACECULLS(VERTEX_DATA, stride);
		if (facecull == 2 && A > 0) return RETURN_VALUE;
		else if (facecull == 1 && A < 0) return RETURN_VALUE;
	}

	if (isWire)
	{
		//DrawWireFrame(&data);
		return RETURN_VALUE;
	}

	int yMin = (int)yMinValue, yMax = (int)yMaxValue;";
            }
        }

        static string ScanLineStart
        {
            get
            {
                return @"float slopeZ, bZ, s;
	float sA, sB;

	float* Intersects = (float*)alloca((4 + (stride - 3) * 5) * 4);
	float* attribs = Intersects + 4 + (stride - 3) * 2;
	float* y_Mxb = attribs + (stride - 3);
	float* y_mxB = y_Mxb + (stride - 3);

	float* FROM;
	float* TO;

	int FromX, ToX;

	int* RGB_iptr;
	float* Z_fptr;

	float zBegin;

	for (int i = yMin; i <= yMax; ++i)
	{
		if (ScanLinePLUS(i, VERTEX_DATA, BUFFER_SIZE, Intersects, stride))
		{
			if (Intersects[0] > Intersects[stride - 1])
			{
				TO = Intersects;
				FROM = Intersects + (stride - 1);
			}
			else
			{
				FROM = Intersects;
				TO = Intersects + (stride - 1);
			}

			FROM[0] = roundf(FROM[0]);
			TO[0] = roundf(TO[0]);

			//Prevent touching faces from fighting over a scanline pixel
			FromX = (int)FROM[0] == 0 ? 0 : (int)FROM[0] + 1;
			ToX = (int)TO[0];

			//integer truncating doesnt matter here as the float values are already rounded

			slopeZ = (FROM[1] - TO[1]) / (FROM[0] - TO[0]);
			bZ = -slopeZ * FROM[0] + FROM[1];

			//we ignore the TO and FROM here so we have proper interpolation within the range
			if (ToX >= renderWidth) ToX = renderWidth - 1;
			if (FromX < 0) FromX = 0;

			float ZDIFF = 1.0f / FROM[1] - 1.0f / TO[1];
			bool usingZ = ZDIFF != 0;
			if (ZDIFF != 0) usingZ = ZDIFF * ZDIFF >= 0.01f;

			if (usingZ)
			for (int b = 0; b < stride - 3; b++)
			{
				sA = (FROM[2 + b] - TO[2 + b]) / ZDIFF;
				sB = -sA / FROM[1] + FROM[2 + b];

				y_Mxb[b] = sA;
				y_mxB[b] = sB;
			}
			else
			for (int b = 0; b < stride - 3; b++)
			{
				sA = (FROM[2 + b] - TO[2 + b]) / (FROM[0] - TO[0]);
				sB = -sA * FROM[0] + FROM[2 + b];

				y_Mxb[b] = sA;
				y_mxB[b] = sB;
			}";
            }
        }

        static string ParallelCode
        {
            get
            {
                return "extern \"C\"" + @" __declspec(dllexport) void ShaderCallFunction(long start, long stop, float* tris, float* dptr, char* uDataVS, char* uDataFS, unsigned char** ptrPtrs, GLData pData, long FACE, long mode)
{
	parallel_for(start, stop, [&](int index){
		MethodExec(index,tris, dptr, uDataVS, uDataFS, ptrPtrs, pData, FACE, mode);
	});
}";
            }
        }

        static string NotParallelCode
        {
            get
            {
                return "extern \"C\"" + @" __declspec(dllexport) void ShaderCallFunction(long start, long stop, float* tris, float* dptr, char* uDataVS, char* uDataFS, unsigned char** ptrPtrs, GLData pData, long FACE, long mode)
{
	for (int i = start; i < stop; i++){
		MethodExec(i,tris, dptr, uDataVS, uDataFS, ptrPtrs, pData, FACE, mode);
	}
}";
            }
        }

        static string ClipCodeSource
        {
            get
            {
                return @"void FIPA(float* TA, int INDEX, float* VD, int A, int B, float LinePos, int Stride)
{
	float X;
	float Y;
	int s = 3;

	A *= Stride;
	B *= Stride;

	if (VD[A + 2] - VD[B + 2] != 0)
	{
		float slopeY = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
		float bY = -slopeY * VD[A + 2] + VD[A + 1];
		Y = slopeY * LinePos + bY;

		float slopeX = (VD[A + 0] - VD[B + 0]) / (VD[A + 2] - VD[B + 2]);
		float bX = -slopeX * VD[A + 2] + VD[A + 0];
		X = slopeX * LinePos + bX;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
			float bA = -slopeA * VD[A + 2] + VD[A + i];
			TA[INDEX + i] = slopeA * LinePos + bA;
		}

	}


	TA[INDEX] = X;
	TA[INDEX + 1] = Y;
	TA[INDEX + 2] = LinePos;
}

void SIPA(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, float oW, int Stride)
{
	float X;
	float Y;
	float Z;

	int s = 3;

	A *= Stride;
	B *= Stride;

	float s1 = VD[A + 2] - VD[B + 2];
	float s2 = (VD[A] - VD[B]);
	s1 *= s1;
	s2 *= s2;

	if (s1 > s2)
	{
		float slope = (VD[A] - VD[B]) / (VD[A + 2] - VD[B + 2]);
		float b = -slope * VD[A + 2] + VD[A];

		Z = (b - oW) / (TanSlope - slope);
		X = Z * slope + b;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
			float bA = -slopeA * VD[A + 2] + VD[A + i];
			TA[INDEX + i] = slopeA * Z + bA;
		}
		s = Stride;
	}
	else
	{
		float slope = (VD[A + 2] - VD[B + 2]) / (VD[A] - VD[B]);
		float b = -slope * VD[A] + VD[A + 2];

		Z = (slope * oW + b) / (1.0f - slope * TanSlope);
		X = TanSlope * Z + oW;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A] - VD[B]);
			float bA = -slopeA * VD[A] + VD[A + i];
			TA[INDEX + i] = slopeA * X + bA;
		}
		s = Stride;
	}

	//Floating point error solution:
	if (s1 > s2)
	{
		float slope = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
		float b = -slope * VD[A + 2] + VD[A + 1];

		Y = slope * Z + b;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
			float bA = -slopeA * VD[A + 2] + VD[A + i];
			TA[INDEX + i] = slopeA * Z + bA;
		}
		s = Stride;
	}
	else
	{
		float slope = (VD[A + 1] - VD[B + 1]) / (VD[A] - VD[B]);
		float b = -slope * VD[A] + VD[A + 1];

		Y = slope * X + b;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A] - VD[B]);
			float bA = -slopeA * VD[A] + VD[A + i];
			TA[INDEX + i] = slopeA * X + bA;
		}
		s = Stride;
	}

	TA[INDEX] = X;
	TA[INDEX + 1] = Y;
	TA[INDEX + 2] = Z;
}

void SIPHA(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, float oH, int Stride)
{
	float X;
	float Y;
	float Z;

	int s = 3;

	A *= Stride;
	B *= Stride;

	//compared to non stride siph, the s1 s2 are flipped; not sure why

	float s1 = VD[A + 2] - VD[B + 2];
	float s2 = VD[A + 1] - VD[B + 1];
	s1 *= s1;
	s2 *= s2;

	if (s2 > s1)
	{
		float slope = (VD[A + 2] - VD[B + 2]) / (VD[A + 1] - VD[B + 1]);
		float b = -slope * VD[A + 1] + VD[A + 2];

		Z = (slope * oH + b) / (1.0f - slope * TanSlope);
		Y = TanSlope * Z + oH;


		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 1] - VD[B + 1]);
			float bA = -slopeA * VD[A + 1] + VD[A + i];
			TA[INDEX + i] = slopeA * Y + bA;
		}
		s = Stride;
	}
	else
	{
		float slope = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
		float b = -slope * VD[A + 2] + VD[A + 1];

		float V = (b - oH) / (TanSlope - slope);

		Y = V * slope + b;
		Z = V;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
			float bA = -slopeA * VD[A + 2] + VD[A + i];
			TA[INDEX + i] = slopeA * Z + bA;
		}
		s = Stride;
	}

	if (s1 > s2)
	{
		float slope = (VD[A] - VD[B]) / (VD[A + 2] - VD[B + 2]);
		float b = -slope * VD[A + 2] + VD[A];

		X = slope * Z + b;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
			float bA = -slopeA * VD[A + 2] + VD[A + i];
			TA[INDEX + i] = slopeA * Z + bA;
		}
		s = Stride;
	}
	else
	{
		float slope = (VD[A] - VD[B]) / (VD[A + 1] - VD[B + 1]);
		float b = -slope * VD[A + 1] + VD[A];

		X = slope * Y + b;

		for (int i = s; i < Stride; i++)
		{
			float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 1] - VD[B + 1]);
			float bA = -slopeA * VD[A + 1] + VD[A + i];
			TA[INDEX + i] = slopeA * Y + bA;
		}
		s = Stride;
	}

	TA[INDEX] = X;
	TA[INDEX + 1] = Y;
	TA[INDEX + 2] = Z;
}

void LIPA_PLUS(float* XR, int I, float* V_DATA, int A, int B, int LinePos, int Stride)
{
	float X;
	float Z;

	A *= Stride;
	B *= Stride;

	if (V_DATA[A + 1] == LinePos)
	{
		XR[I * (Stride - 1)] = V_DATA[A];
		XR[I * (Stride - 1) + 1] = V_DATA[A + 2];

		for (int a = 3; a < Stride; a++)
		{
			XR[I * (Stride - 1) + (a - 1)] = V_DATA[A + a];
		}
		return;
	}

	if (V_DATA[B + 1] == LinePos)
	{
		XR[I * (Stride - 1)] = V_DATA[B];
		XR[I * (Stride - 1) + 1] = V_DATA[B + 2];

		for (int a = 3; a < Stride; a++)
		{
			XR[I * (Stride - 1) + (a - 1)] = V_DATA[B + a];
		}
		return;
	}

	if (V_DATA[A + 1] - V_DATA[B + 1] != 0)
	{
		float slope = (V_DATA[A] - V_DATA[B]) / (V_DATA[A + 1] - V_DATA[B + 1]);
		float b = -slope * V_DATA[A + 1] + V_DATA[A];
		X = slope * LinePos + b;

		float slopeZ = (V_DATA[A + 2] - V_DATA[B + 2]) / (V_DATA[A + 1] - V_DATA[B + 1]);
		float bZ = -slopeZ * V_DATA[A + 1] + V_DATA[A + 2];
		Z = slopeZ * LinePos + bZ;

	}


	float ZDIFF = (1.0f / V_DATA[A + 2] - 1.0f / V_DATA[B + 2]);
	bool usingZ = ZDIFF != 0;

	if (ZDIFF != 0)
		usingZ = ZDIFF * ZDIFF >= 0.00001f;

	if (usingZ)
	for (int a = 3; a < Stride; a++)
	{
		float slopeA = (V_DATA[A + a] - V_DATA[B + a]) / ZDIFF;
		float bA = -slopeA / V_DATA[A + 2] + V_DATA[A + a];
		XR[I * (Stride - 1) + (a - 1)] = slopeA / Z + bA;
	}
	else if (V_DATA[A + 1] - V_DATA[B + 1] != 0)
	for (int a = 3; a < Stride; a++)
	{
		float slopeA = (V_DATA[A + a] - V_DATA[B + a]) / (V_DATA[A + 1] - V_DATA[B + 1]);
		float bA = -slopeA * V_DATA[A + 1] + V_DATA[A + a];
		XR[I * (Stride - 1) + (a - 1)] = (slopeA * (float)LinePos + bA);


	}


	XR[I * (Stride - 1) + 0] = X;
	XR[I * (Stride - 1) + 1] = Z;
}

bool ScanLinePLUS(int Line, float* TRIS_DATA, int TRIS_SIZE, float* Intersects, int Stride)
{
	int IC = 0;
	for (int i = 0; i < TRIS_SIZE - 1; i++)
	{
		float y1 = TRIS_DATA[i * Stride + 1];
		float y2 = TRIS_DATA[(i + 1) * Stride + 1];

		if (y2 == y1 && Line == y2){
			LIPA_PLUS(Intersects, 0, TRIS_DATA, i, i + 1, Line, Stride);
			LIPA_PLUS(Intersects, 1, TRIS_DATA, i + 1, i, Line, Stride);
			return true;
		}

		if (y2 < y1){
			float t = y2;
			y2 = y1;
			y1 = t;
		}

		if (Line <= y2 && Line > y1){
			LIPA_PLUS(Intersects, IC, TRIS_DATA, i, i + 1, Line, Stride);
			IC++;
		}

		if (IC >= 2) return true;
	}

	if (IC < 2)
	{
		float y1 = TRIS_DATA[0 * Stride + 1];
		float y2 = TRIS_DATA[(TRIS_SIZE - 1) * Stride + 1];

		if (y2 == y1 && Line == y2){
			LIPA_PLUS(Intersects, 0, TRIS_DATA, 0, (TRIS_SIZE - 1), Line, Stride);
			LIPA_PLUS(Intersects, 1, TRIS_DATA, (TRIS_SIZE - 1), 0, Line, Stride);
			return true;
		}

		if (y2 < y1){
			float t = y2;
			y2 = y1;
			y1 = t;
		}

		if (Line <= y2 && Line > y1){
			LIPA_PLUS(Intersects, IC, TRIS_DATA, 0, TRIS_SIZE - 1, Line, Stride);
			IC++;
		}
	}

	if (IC == 2) return true;
	else return false;
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

inline void frtlzeromem(bool* dest, int count)
{
	for (int i = 0; i < count; ++i)
		dest[i] = false;
}

inline float BACKFACECULLS(float* VERTEX_DATA, int Stride)
{
	return ((VERTEX_DATA[Stride]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[Stride * 2 + 1]) - (VERTEX_DATA[1])) - ((VERTEX_DATA[Stride * 2]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[Stride + 1]) - (VERTEX_DATA[1]));
}";
            }
        }

        //->

        static string WriteMethods(ShaderMethod[] methods, ShaderField[] outFields, ShaderField[] inFields, ShaderField[] uniforms)
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

                for (int o = 0; o < outFields.Length; o++)
                {
                    if (ContainsName(methods[i].contents, outFields[o].name))
                        throw new Exception("XFDraw Parser does not support having fields inside other methods. Please copy them manually!");
                }

                for (int o = 0; o < inFields.Length; o++)
                {
                    if (ContainsName(methods[i].contents, inFields[o].name))
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

            if (char.IsLetterOrDigit(first) || char.IsLetterOrDigit(secnd) || first == '*' || first == '_' || secnd == '_')
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

                if (char.IsLetterOrDigit(first) || char.IsLetterOrDigit(secnd) || first == '*' || first == '_' || secnd == '_')
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

        static void PrepVSData(ShaderField[] shaderFields, out ShaderField[] vsIn, out ShaderField[] vsOut, out int readS, out int iS)
        {
            int count = 0;

            List<ShaderField> vsI = new List<ShaderField>();
            List<ShaderField> vsO = new List<ShaderField>();

            for (int i = 0; i < shaderFields.Length; i++)
            {
                if (shaderFields[i].dataMode == DataMode.In) vsI.Add(shaderFields[i]);
                else if (shaderFields[i].dataMode == DataMode.Out) vsO.Add(shaderFields[i]);
                else throw new Exception("An error occured (9582)");
            }

            for (int i = 0; i < vsI.Count; i++)
                count += vsI[i].layoutValueGL != -1 ? 1 : 0;

            if (!(count == 0 || count == vsI.Count))
                throw new Exception("VS layout not set correctly!");

            if (count != 0)
            {
                vsI.Sort(delegate(ShaderField x, ShaderField y)
                {
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

        static void PrepFSData(ShaderField[] shaderFields, out ShaderField[] fsIn, out ShaderField[] fsOut)
        {
            int count = 0;

            List<ShaderField> fsI = new List<ShaderField>();
            List<ShaderField> fsO = new List<ShaderField>();

            for (int i = 0; i < shaderFields.Length; i++)
            {
                if (shaderFields[i].dataMode == DataMode.In) fsI.Add(shaderFields[i]);
                else if (shaderFields[i].dataMode == DataMode.Out) fsO.Add(shaderFields[i]);
                else throw new Exception("An error occured (9582)");
            }

            for (int i = 0; i < fsI.Count; i++)
                count += fsI[i].layoutValueGL != -1 ? 1 : 0;

            if (count != 0)
                throw new Exception("FS layout not supported!");

            int offset = 0;
            for (int i = 0; i < fsI.Count; i++)
            {
                fsI[i].layoutPosition = offset;
                offset += fsI[i].GetSize();
            }

            int offset1 = 0;
            for (int i = 0; i < fsO.Count; i++)
            {
                fsO[i].layoutPosition = offset1;
                offset1 += fsO[i].GetSize();
            }

            fsIn = fsI.ToArray();
            fsOut = fsO.ToArray();
        }

        static string WriteExecMethod(ShaderParser data, bool wrapGLPos = false)
        {
            string mainCode = Regex.Replace(data.shaderMethods[data.shaderMethods.Length - 1].contents, ";", ";\n\t");
            mainCode = Regex.Replace(mainCode, "{", "{\n\t");
            mainCode = "\t" + Regex.Replace(mainCode, "}", "}\n\t");

            for (int i = 0; i < data.shaderFieldIns.Length; i++)
                mainCode = WrapVariablePointer(mainCode, data.shaderFieldIns[i].name);

            for (int i = 0; i < data.shaderFieldOuts.Length; i++)
                mainCode = WrapVariablePointer(mainCode, data.shaderFieldOuts[i].name);




            if (wrapGLPos)
                mainCode = WrapVariablePointer(mainCode, "gl_Position");

            return mainCode;
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

                methodExec += "*(" + type + "*)(uData1 + " + data.shaderUniforms[i].layoutPosition + "), ";
                methodSign += type + " " + data.shaderUniforms[i].name + ", ";
            }


            exec = methodExec.Substring(0, methodExec.Length - 2) + ");";
            sign = methodSign.Substring(0, methodSign.Length - 2) + ")";
        }

        static void WriteFSSign(ShaderParser data, ShaderField[] fsIn, ShaderField[] fsOut, out string sign, out string exec, out string ptrs, out string ptrsinc)
        {
            string methodSign = "inline void FSExec(";
            string methodExec = "FSExec(";
            string ptrsDecl = "";
            string ptrsIncr = "";

            for (int i = 0; i < fsOut.Length; i++)
            {
                string type = TypeToString(fsOut[i].dataType);
                methodExec += "ptr_" + i + " + o, ";
                methodSign += type + "* " + fsOut[i].name + ", ";

                ptrsDecl += type + "* " + "ptr_" + i + " = (" + type + "*)(ptrPtrs[" + i + "] + wPos * " + TypeToSize(fsOut[i].dataType) + ");\n";
                ptrsIncr += "++ptr_" + i + ", ";
            }

            for (int i = 0; i < fsIn.Length; i++)
            {
                string type = TypeToString(fsIn[i].dataType);
                methodExec += "(" + type + "*)(attribs + " + (fsIn[i].layoutPosition / 4) + "), ";
                methodSign += type + "* " + fsIn[i].name + ", ";
            }


            for (int i = 0; i < data.shaderUniforms.Length; i++)
            {
                string type = data.shaderUniforms[i].dataType != DataType.Other ? TypeToString(data.shaderUniforms[i].dataType) : data.shaderUniforms[i].typeName;

                methodExec += "*(" + type + "*)(uData2 + " + data.shaderUniforms[i].layoutPosition + "), ";
                methodSign += type + " " + data.shaderUniforms[i].name + ", ";
            }

            if (methodExec.Length > 2)
                methodExec = methodExec.Substring(0, methodExec.Length - 2) + ");";

            if (methodSign.Length > 2)
                methodSign = methodSign.Substring(0, methodSign.Length - 2) + ")";

            if (ptrsIncr.Length > 2)
                ptrsIncr = ptrsIncr.Substring(0, ptrsIncr.Length - 2);


            sign = methodSign;
            exec = methodExec;
            ptrs = ptrsDecl;
            ptrsinc = ptrsIncr;
        }

    }

    [Serializable]
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
            else if (input == "sampler2D") return DataType.sampler2D;

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
            else if (dataType == DataType.sampler2D) return 20;
            else if (dataType == DataType.Other && FieldSize != -1) return FieldSize;
            else throw new Exception("An unknown error occured (00245)");
        }

        internal void SetLayoutOffset(int value)
        {
            layoutPosition = value;
        }
    }

    [Serializable]
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

    [Serializable]
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

    [Serializable]
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
        sampler2D,
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
        ForceRecompile,
        DoNotSerialize,
        EnableMSAASupport,
        EnableSIMD,
        UseOMP,
        UsePPL,
        UseFor
    }
}
