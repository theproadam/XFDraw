using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace RemoteCompile
{
    public class ShaderCompile
    {
        public static string COMPILER_LOCATION = @"C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\bin\";
        public static string COMPILER_NAME = "cl.exe";

        ShaderField[] sFieldsVS;
        ShaderMethod[] sMethodsVS;
        ShaderStruct[] sStructsVS;

        ShaderField[] sFieldsFS;
        ShaderMethod[] sMethodsFS;
        ShaderStruct[] sStructsFS;
        bool isScreenSpace = false;

        bool requireSXY; bool requireFaceIndx;

        internal ShaderCompile(ShaderParser VS, ShaderParser FS, bool isScreen)
        {
            if (!isScreen) { 
                sFieldsVS = VS.shaderFields;
                sMethodsVS = VS.shaderMethods;
                sStructsVS = VS.shaderStructs;
            }

            sFieldsFS = FS.shaderFields;
            sMethodsFS = FS.shaderMethods;
            sStructsFS = FS.shaderStructs;

            isScreenSpace = isScreen;
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


        public void Do(string FileSource)
        {
            if (!File.Exists(FileSource))
                throw new FileNotFoundException("Cannot compile Shader!");

            string path = System.AppDomain.CurrentDomain.BaseDirectory;
            string cmd = @"/EHsc /Fo """ + path + "" + FileSource + "\"";

            //throw new Exception();
           // Console.WriteLine("-> " + (COMPILER_LOCATION + COMPILER_NAME) + " " + cmd);

            string tempPath = path;
            string tempName = FileSource;


            Process compiler = new Process();

            compiler.StartInfo.FileName = "cmd.exe";
            compiler.StartInfo.WorkingDirectory = tempPath;
            compiler.StartInfo.RedirectStandardInput = true;
            compiler.StartInfo.RedirectStandardOutput = true;
            compiler.StartInfo.UseShellExecute = false;

            compiler.Start();

            compiler.StandardInput.WriteLine("\"" + @"C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\bin\vcvars32.bat" + "\"");
            compiler.StandardInput.WriteLine(@"cl.exe /EHsc /LD " + tempName);
            compiler.StandardInput.WriteLine(@"exit");



            string[] outputFile = FileSource.Split('.');

            if (!File.Exists(path + outputFile[outputFile.Length - 2] + ".dll"))
            {
                Console.WriteLine("Failed To Compile: ");
                string output = compiler.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
            }

            compiler.WaitForExit();
            compiler.Close();


        }
    }

    public class ShaderParser
    {
        internal ShaderField[] shaderFields;
        internal ShaderMethod[] shaderMethods;
        internal ShaderStruct[] shaderStructs;

        ShaderParser(ShaderField[] f, ShaderMethod[] m, ShaderStruct[] s)
        {
            shaderFields = f;
            shaderMethods = m;
            shaderStructs = s;
        }

        
        public static bool Compile(string vertexShader, string fragmentShader, out ShaderCompile shaderModule, params CompileOption[] compileOptions)
        {
            if (!File.Exists(vertexShader) || !File.Exists(fragmentShader))
                throw new FileNotFoundException();

            string[] analyzeVS = File.ReadAllLines(vertexShader);
            string VSReady = PrepareInput(analyzeVS);
            ShaderParser vsModule = Parse(VSReady);

            string[] analyzeFS = File.ReadAllLines(fragmentShader);
            string FSReady = PrepareInput(analyzeFS);
            ShaderParser fsModule = Parse(FSReady);

            shaderModule = new ShaderCompile(vsModule, fsModule, false);

            WriteShaders(vertexShader, fragmentShader);

            return true;
        }

        public static string PrepareInput(string[] str)
        { 
            str = RemoveComments(str);
            string s = FlattenString(str);
            return RemoveBlockComments(s);
        }

        public static bool Compile(string screenspaceShader, out ShaderCompile shaderModule, params CompileOption[] compileOptions)
        {
            if (!File.Exists(screenspaceShader))
                throw new FileNotFoundException();

            string[] analyzeS = File.ReadAllLines(screenspaceShader);
            string SReady = PrepareInput(analyzeS);
            ShaderParser sModule = Parse(SReady);

            shaderModule = new ShaderCompile(null, sModule, true);


            WriteShader(screenspaceShader, sModule, compileOptions);

            return true;
        }

        static void WriteShaders(string filePath1, string filePath2)
        {
            string ext = filePath1.Split('.')[0] + "_" + filePath2.Split('.')[0];

            string foldername = "_temp_" + ext;

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

        static void WriteShader(string filePath, ShaderParser data, CompileOption[] cOps)
        {
            string foldername = "_temp_" + filePath.Split('.')[0];

            if (!Directory.Exists(foldername))
                Directory.CreateDirectory(foldername);

            if (File.Exists(foldername + @"\" + filePath))
            {
                if (File.ReadLines(filePath).SequenceEqual(File.ReadLines(foldername + @"\" + filePath)) && !cOps.Contains(CompileOption.ForceRecompile))
                    return;

                File.Delete(foldername + @"\" + filePath);
            }

            File.Copy(filePath, foldername + @"\" + filePath);

            List<string> ptrs = new List<string>();

            string ptrsIncr = ""; //Pointers that are incremented
            string methdSign = ""; //Method Signature for pointers

            string methdSignUnifm = ""; //Method signature for uniforms

            string indt = "\t\t"; //indent level 1
            string indt1 = "\t"; //indent level 2

            string uFields = ""; //uniform field and data copying

            string execSignPtr = ""; //inline void shaderMethod(etc etc)
            string execSignUni = ""; //inline void shaderMethod(etc etc)

            if (!data.shaderMethods[data.shaderMethods.Length - 1].isEntryPoint)
                throw new Exception("Expected entry point!");

            string mainCode = data.shaderMethods[data.shaderMethods.Length - 1].contents;
            
            for (int i = 0; i < data.shaderFields.Length; i++)
            {
                if (data.shaderFields[i].dataMode == DataMode.Uniform)
                {
                    if (data.shaderFields[i].dataType == DataType.vec2)
                    {
                        uFields += indt1 + "vec2 " + "uniform_" + i + ";\n";
                        uFields += indt1 + "fcpy((char*)(&uniform_" + i + "), (char*)UniformPointers, 8);\n";

                        methdSignUnifm += "uniform_" + i + ", ";
                        execSignUni += "vec2 " + data.shaderFields[i].name + ", ";
                    }
                    else throw new NotImplementedException();
                    continue;
                }

                if (data.shaderFields[i].dataType == DataType.byte4 || data.shaderFields[i].dataType == DataType.int32)
                {
                    ptrs.Add(indt + "int* " + "ptr_" + i + " = (int*)wPos;\n");
                    ptrsIncr += ", ++ptr_" + i;
                    methdSign += "ptr_" + i + ", ";
                    execSignPtr += data.shaderFields[i].dataType == DataType.byte4 ? "byte4* " : "int* " + data.shaderFields[i].name + ", ";
                }
                else if (data.shaderFields[i].dataType == DataType.fp32)
                {
                    ptrs.Add(indt + "float* " + "ptr_" + i + " = (float*)wPos;\n");
                    ptrsIncr += ", ++ptr_" + i;
                    methdSign += "ptr_" + i + ", ";
                    execSignPtr += "float* " + data.shaderFields[i].name + ", ";
                }
                else
                {
                    throw new NotImplementedException("not yet!");
                }
            }


            mainCode = Regex.Replace(mainCode, ";", ";\n\t");
            mainCode = Regex.Replace(mainCode, "{", "{\n\t");
            mainCode = "\t" + Regex.Replace(mainCode, "}", "}\n\t");
            


            execSignUni = execSignUni.Substring(0, execSignUni.Length - 2);

            List<string> shaderOutput = new List<string>();
            shaderOutput.Add("//Autogenerated by XFDraw shader parser");

            shaderOutput.Add("inline void shaderMethod(" + execSignPtr + execSignUni + "){");

            shaderOutput.Add(mainCode + "\n}");

            //Add Base Part
            const string entry = "extern \"C\" __declspec(dllexport) void ";
            shaderOutput.Add(entry + "ShaderCallFunction(long Width, long Height, void* UniformPointers, long UniformCount){");

            shaderOutput.Add(uFields);

            shaderOutput.Add("#pragma omp parallel for");
            shaderOutput.Add("\tfor (int h = 0; h < Height; ++h){");
            shaderOutput.AddRange(ptrs);

            shaderOutput.Add("\t\tfor (int w = 0; w < Width; ++w" + ptrsIncr + "){");
            shaderOutput.Add("\t\t\tshaderMethod(" + methdSign + methdSignUnifm.Substring(0, methdSignUnifm.Length - 2) + ");\n\t\t}\n\t}");

            shaderOutput.Add("}");

            File.WriteAllLines(foldername + @"\" + filePath.Split('.')[0] + "_merged.cpp", shaderOutput.ToArray());

        }

        static void CheckForDependencies(string input)
        {
            if (input.Contains("gl_FragCoord"))
            { 
                
            }


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

        static void ParseOLD(string str)
        {
            str = Regex.Replace(str, @"\s+", " ");

            string mainMethod = "";

            List<string> inititems = new List<string>();
            List<string> structs = new List<string>();
            List<string> methods = new List<string>();


            string buildstr = "";
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '{')
                {
                    if (buildstr.StartsWith("void main()"))
                    {
                        int frm = i + 1;
                        ReadMethod(i + 1, str, out i);
                        mainMethod = str.Substring(frm, i - frm - 1);
                        buildstr = "";

                        if (i >= str.Length) break;
                    }
                    else if (buildstr.StartsWith("struct "))
                    {
                        int frm = i + 1;
                        ReadMethod(i + 1, str, out i);
                        structs.Add(buildstr + str.Substring(frm, i - frm - 1));
                        buildstr = "";

                        i++; //remove trailing: ;
                        if (i >= str.Length) break;
                    }
                    else if (buildstr.StartsWith("inline main()"))
                    {
                        throw new Exception("Please do not manually inline functions!");
                    }
                    else
                    {
                        int frm = i + 1;
                        ReadMethod(i + 1, str, out i);
                        methods.Add(buildstr + str.Substring(frm, i - frm - 1));
                        buildstr = "";

                        if (i >= str.Length) break;
                    }
                }

                if (str[i] == ';')
                {
                    inititems.Add(buildstr);
                    buildstr = "";
                }
                else buildstr += str[i];
            }

            throw new Exception();

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

                    if (!shaderStructs.Any(shaderStruct => shaderStruct.structName == shaderFields[i].otherName))
                        throw new Exception("Could not find struct called \"" + shaderFields[i].otherName + "\" for \"" + shaderFields[i].name + "\"");
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
    }

    internal struct ShaderField
    {
        internal string name;
        internal DataType dataType;
        internal DataMode dataMode;

        internal string otherName;

        internal int layoutValue;
        internal int FieldSize;

        public ShaderField(string inputString)
        {
            string[] str = inputString.Trim().Split(' ');

            if (str.Length != 3) throw new Exception("Unknown Data!");

            name = str[2];
            dataType = toDataType(str[1]);
            dataMode = toDataMode(str[0]);
            layoutValue = -1;
            FieldSize = dataType != DataType.Other ? (int)dataType : -1;
            otherName = dataType == DataType.Other ? str[1] : "";
        }

        internal static DataType toDataType(string input)
        {
            if (input == "byte4") return DataType.byte4;
            else if (input == "float") return DataType.fp32;
            else if (input == "int2") return DataType.int2;
            else if (input == "int32") return DataType.int32;
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

    }

    internal struct ShaderMethod
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

    internal struct ShaderStruct
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

    internal struct StructField
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
