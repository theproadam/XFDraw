using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using xfcore.Buffers;
using xfcore.Shaders.Structs;

namespace xfcore.Shaders
{
    public unsafe class ShaderModule
    {
        internal void* VSptr;
        internal void* FSptr;

        internal int VSSize;
        internal int FSSize;

        internal ShaderModule(void* vptr, void* fptr, int vs, int fs)
        {
            VSptr = vptr;
            FSptr = fptr;

            VSSize = vs;
            FSSize = fs;
        }
    }

    public unsafe partial class Shader
    {
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
        static extern int LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
        static extern IntPtr GetProcAddress(int hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        static extern bool FreeLibrary(int hModule);

        public static bool Override_Padding_Warning = false;

        public static Shader Load(ShaderModule sModule, Type vsStruct, Type fsStruct)
        {
            FieldInfo[] VSFields = vsStruct.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo[] FSFields = fsStruct.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (sModule.VSSize == -1)
                throw new Exception("Expected regular shader, got screen space shader!");

            if (!vsStruct.IsLayoutSequential)
                throw new Exception("Vertex Shader Struct Must Be LayoutKind.Sequential!");

            if (!fsStruct.IsLayoutSequential)
                throw new Exception("Pixel Shader Struct Must Be LayoutKind.Sequential!");

            if (Marshal.SizeOf(vsStruct) != sModule.VSSize)
                throw new Exception("Vertex Shader struct size is not the same as the one in the shader module!");

            if (Marshal.SizeOf(fsStruct) != sModule.FSSize)
                throw new Exception("Pixel Shader struct size is not the same as the one in the shader module!");

            for (int i = 0; i < VSFields.Length; i++)
            {
                if (Attribute.IsDefined(VSFields[i], typeof(Structs.xout)) | Attribute.IsDefined(VSFields[i], typeof(Structs.xinp)))
                    if (!VSFields[i].FieldType.IsPointer)
                        throw new Exception("In or Out types must be pointer types!");
            }
            for (int i = 0; i < FSFields.Length; i++)
            {
                if (Attribute.IsDefined(FSFields[i], typeof(Structs.xout)) | Attribute.IsDefined(FSFields[i], typeof(Structs.xinp)))
                    if (!FSFields[i].FieldType.IsPointer)
                        throw new Exception("In or Out types must be pointer types!");
            }
            
            //check valid VS_OUT and VS_IN
            int VOUT_pos = -1;

            for (int i = 0; i < VSFields.Length; i++)
            {
                if (Attribute.IsDefined(VSFields[i], typeof(Structs.VS_out)))
                {
                    if (VOUT_pos != -1)
                        throw new Exception("Error: Two VS_out fields cannot exist!");
                    VOUT_pos = i;

                    if (Marshal.SizeOf(VSFields[i].FieldType.GetElementType()) != 12)
                        throw new Exception("VS_OUT needs to be a vector3!");
                }
            }

            if (VOUT_pos == -1)
                throw new Exception("VS_out field required in vertex shader!");

            //compare outs and ins
            List<Type> TypeDataVS = new List<Type>();
            List<int> PosiDataVS = new List<int>();
            int c1 = 0;

            for (int i = 0; i < VSFields.Length; i++)
            {
                if (Attribute.IsDefined(VSFields[i], typeof(Structs.xout)))
                {
                    TypeDataVS.Add(VSFields[i].FieldType);
                    PosiDataVS.Add(c1);
                    c1++;
                }
            }

            List<Type> TypeDataFS = new List<Type>();
            List<int> PosiDataFS = new List<int>();
            c1 = 0;

            for (int i = 0; i < FSFields.Length; i++)
            {
                if (Attribute.IsDefined(FSFields[i], typeof(Structs.xinp)))
                {
                    TypeDataFS.Add(FSFields[i].FieldType);
                    PosiDataFS.Add(c1);
                    c1++;
                }
            }

            if (TypeDataFS.Count != TypeDataVS.Count)
                throw new Exception("Vertex Shader Out's have to be the same count as the Fragment Shader In's!");

            for (int i = 0; i < TypeDataFS.Count; i++)
            {
                if (TypeDataFS[i] != TypeDataVS[i])
                    throw new Exception("Vertex Shader Out's have to be the same type as the Fragment Shader In's!");

                if (PosiDataVS[i] != PosiDataFS[i])
                    throw new Exception("Vertex Shader Out's positions have to align with the Fragment Shader In's!");
            }


            return new Shader(sModule.VSptr, sModule.FSptr, VSFields, FSFields, vsStruct, fsStruct, sModule.VSSize, sModule.FSSize);
        }

        public static string GetShaderData(Shader inputShader)
        {
            string ReturnString = "";
            if (!inputShader.isScreenSpace)
            {
                FieldInfo[] fin = inputShader.VSData;
                ReturnString += "Vertex Structure:\n";

                for (int i = 0; i < fin.Length; i++){
                    int v = Marshal.SizeOf(fin[i].FieldType);
                    ReturnString += fin[i].Name + ", Type: " + fin[i].FieldType.ToString() + ", Size: " + v + "\n";
                }
            }

            FieldInfo[] fin1 = inputShader.VSData;
            ReturnString += "Pixel Structure:\n";

            for (int i = 0; i < fin1.Length; i++)
            {
                int v = Marshal.SizeOf(fin1[i].FieldType);
                ReturnString += fin1[i].Name + ", Type: " + fin1[i].FieldType.ToString() + ", Size: " + v + "\n";
            }

            return ReturnString;
        }

        public static Shader Load(ShaderModule shaderModule, Type ScreenSpaceShaderStruct)
        {
            FieldInfo[] FSFields = ScreenSpaceShaderStruct.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (shaderModule.VSSize != -1)
                throw new Exception("Expected Screen Space Shader, got normal shader!");

            if (!ScreenSpaceShaderStruct.IsLayoutSequential)
                throw new Exception("Vertex Shader Struct Must Be LayoutKind.Sequential!");

            int s = Marshal.SizeOf(ScreenSpaceShaderStruct);

            if (s != shaderModule.FSSize)
                throw new Exception("Pixel Shader struct size is not the same as the one in the shader module!");

            for (int i = 0; i < FSFields.Length; i++)
            {
                if (Attribute.IsDefined(FSFields[i], typeof(Structs.xout)) | Attribute.IsDefined(FSFields[i], typeof(Structs.xinp)))
                    if (!FSFields[i].FieldType.IsPointer)
                        throw new Exception("In or Out types must be pointer types!");
            }
            return new Shader(shaderModule.FSptr, FSFields, shaderModule.FSSize, ScreenSpaceShaderStruct);
        }

        public static bool LoadModules(string ShaderPath, out ShaderModule[] Output)
        {
            Output = null;
            if (!File.Exists(ShaderPath)) return false;

            int hModule = LoadLibrary(ShaderPath);
            if (hModule == 0) return false;

            IntPtr InitializeTrigger = GetProcAddress(hModule, "InitializeDLLModule");
            IntPtr FreeMemoryTrigger = GetProcAddress(hModule, "FreeMalloc");

            if (InitializeTrigger == IntPtr.Zero || FreeMemoryTrigger == IntPtr.Zero) return false;

            Output = ConvertToShaders(InitializeTrigger, FreeMemoryTrigger);

            return true;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int* ShaderDelegate(int* p);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void FreeMemDelgate();

        internal static ShaderModule[] ConvertToShaders(IntPtr ActivationFunction, IntPtr ReleaseFunction)
        {
            ShaderDelegate GetShaderData = (ShaderDelegate)Marshal.GetDelegateForFunctionPointer(ActivationFunction, typeof(ShaderDelegate));
            FreeMemDelgate ReleaseShaderData = (FreeMemDelgate)Marshal.GetDelegateForFunctionPointer(ReleaseFunction, typeof(FreeMemDelgate));

            int ShaderCount;
            int* m = GetShaderData(&ShaderCount);

            List<ShaderModule> ExtractedShaders = new List<ShaderModule>();

            for (int i = 0; i < ShaderCount; i++)
            {
                int Vptr = m[i * 4 + 1]; if (Vptr == 0) throw new Exception("Invalid Vertex Shader Pointer In LoadModule()");
                int Fptr = m[i * 4 + 3]; if (Fptr == 0) throw new Exception("Invalid Pixel Shader Pointer In LoadModule()");
                int Vs = m[i * 4 + 0]; if (Vs == 0) throw new Exception("Vertex Shader Struct Size Cannot Be Zero");
                int Fs = m[i * 4 + 2]; if (Fs == 0) throw new Exception("Pixel Shader Struct Size Cannot Be Zero");

                if (Vs != -1 && Vs % 4 != 0 && !Override_Padding_Warning) 
                    throw new Exception("WARNING: Vertex Shader Struct is not 4 byte multiple, padding errors may occur!.\nThis Error can be overwritten.");

                if (Fs % 4 != 0 && !Override_Padding_Warning)
                    throw new Exception("WARNING: Pixel Shader Struct is not 4 byte multiple, padding errors may occur!.\nThis Error can be overwritten.");

                ExtractedShaders.Add(new ShaderModule((void*)Vptr, (void*)Fptr, Vs, Fs));

            }
            ReleaseShaderData();

            return ExtractedShaders.ToArray();
        }
    }

    public unsafe partial class Shader
    {
        internal void* VSptr;
        internal void* FSptr;

        internal int VSS;
        internal int FSS;

        internal FieldInfo[] VSData;
        internal FieldInfo[] FSData;

        Type VSStruct;
        Type FSStruct;

        internal DataItem[] VSD;
        internal DataItem[] FSD;

        internal bool isScreenSpace = false;
        public bool EnableAlphaBlending = false;

        internal Shader(void* vptr, void* fptr, FieldInfo[] VS, FieldInfo[] FS, Type VSType, Type FSType, int VSize, int FSize)
        {
            VSptr = vptr;
            FSptr = fptr;
            VSData = VS;
            FSData = FS;

            VSS = VSize;
            FSS = FSize;

            VSStruct = VSType;
            FSStruct = FSType;

            VSD = new DataItem[VS.Length];
            FSD = new DataItem[FS.Length];
        }

        internal Shader(void* sptr, FieldInfo[] SS, int FSize, Type FSSt)
        {
            FSptr = sptr;
            FSData = SS;
            isScreenSpace = true;

            FSS = FSize;
            FSStruct = FSSt;
            FSD = new DataItem[SS.Length];
        }

        /// <summary>
        /// Sets a uniform constant value in the shader
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="value">The item that will be set</param>
        public void SetValue(string propertyName, object value)
        {
            int s = 0;

            if (value == null) throw new Exception("Value cannot be null!");

            if (value.GetType() == typeof(GLTexture))
            {
                GLTexture t = (GLTexture)value;
                value = new sampler2D(t.Width, t.Height, (int*)t.HEAP_ptr); // WARNING THIS CAN BE A SERIOUS ISSUE HERE!!
                throw new Exception("fix the above issue before!!");
            }

            if (!value.GetType().IsValueType)
                throw new Exception("Value needs to be a struct!");
  

            if (!isScreenSpace)
                for (int i = 0; i < VSData.Length; i++)
                    if (VSData[i].Name == propertyName)
                    {
                        if (Marshal.SizeOf(VSData[i].FieldType) != Marshal.SizeOf(value))
                            throw new Exception("\'" + VSData[i].Name + "\' is not the same size as \'" + value.GetType() + "\'");

                        if (Attribute.IsDefined(VSData[i], typeof(Structs.uniform)))
                        {
                            VSD[i].AssignedValue = value;
                            VSD[i].Size = Marshal.SizeOf(value);
                            VSD[i].bytePosition = (int)Marshal.OffsetOf(VSStruct, propertyName);

                            s++;
                        }
                        else throw new Exception("You cannot SetValue() to a non-uniform type");
                    }

            for (int i = 0; i < FSData.Length; i++)
                if (FSData[i].Name == propertyName) 
                {
                    if (Marshal.SizeOf(FSData[i].FieldType) != Marshal.SizeOf(value))
                        throw new Exception("\'" + FSData[i].Name + "\' is not the same size as \'" + value.GetType() + "\'");

                    if (Attribute.IsDefined(FSData[i], typeof(Structs.uniform)))
                    {
                        FSD[i].AssignedValue = value;
                        FSD[i].Size = Marshal.SizeOf(value);
                        FSD[i].bytePosition = (int)Marshal.OffsetOf(FSStruct, propertyName);

                        s++;
                    }
                    else throw new Exception("You cannot SetValue() to a non-uniform type");
                }

            if (s >= 2) throw new Exception("Error: Multiple \'" + propertyName + "\' detected");
            if (s == 0) throw new Exception("Error: No \'" + propertyName + "\' items are present!");
        }

        public void AssignBuffer(string propertyName, GLBuffer value, int readOffset = 0)
        {
            if (readOffset < 0 || readOffset > value.stride) throw new Exception("Invalid ReadOffset");

            int s = 0;

            for (int i = 0; i < VSData.Length; i++)
                if (VSData[i].Name == propertyName)
                {
                    if (Attribute.IsDefined(VSData[i], typeof(Structs.xinp)))
                    {
                        if (Marshal.SizeOf(VSData[i].FieldType.GetElementType()) / 4 + readOffset > value.stride)
                            throw new Exception("\'" + VSData[i].Name + "\' is not the same size/stride as \'" + value.GetType() + "\'");

                        VSD[i].AssignedBuffer = value;
                        VSD[i].Size = value.stride;
                        VSD[i].bytePosition = (int)Marshal.OffsetOf(VSStruct, propertyName);

                        if (Attribute.IsDefined(VSData[i], typeof(Structs.xinp))) VSD[i].CustomInOutUniform = (int)DataItemType.In;

                        s++;
                    }
                    else throw new Exception("You cannot AssignProperty() to a non in or out type");
                }

            if (s >= 2) throw new Exception("Error: Multiple \'" + propertyName + "\' detected");
            if (s == 0) throw new Exception("Error: No \'" + propertyName + "\' detected");
        }

        public void AssignBuffer(string propertyName, GLTexture value)
        {
            //if (!isScreenSpace) throw new Exception("AssignProperty is only for ScreenSpace Shaders!");

            int s = 0;

            for (int i = 0; i < FSData.Length; i++)
                if (FSData[i].Name == propertyName)
                {
                    if (Attribute.IsDefined(FSData[i], typeof(Structs.xinp)) || Attribute.IsDefined(FSData[i], typeof(Structs.xout)))
                    {
                        if (Marshal.SizeOf(FSData[i].FieldType.GetElementType()) != value.Stride)
                            throw new Exception("\'" + FSData[i].Name + "\' (" + FSData[i].FieldType.GetElementType().ToString()
                                + ") is not the same size/stride as \'" + value.Stride + "\'");

                        FSD[i].AssignedTexture = value;
                        FSD[i].Size = value.Stride;
                        FSD[i].bytePosition = (int)Marshal.OffsetOf(FSStruct, propertyName);

                        if (Attribute.IsDefined(FSData[i], typeof(Structs.xinp))) FSD[i].CustomInOutUniform = (int)DataItemType.In;
                        if (Attribute.IsDefined(FSData[i], typeof(Structs.xout))) FSD[i].CustomInOutUniform = (int)DataItemType.Out;

                        s++;
                    }
                    else throw new Exception("You cannot AssignProperty() to a non in or out type");
                }

            if (s >= 2) throw new Exception("Error: Multiple \'" + propertyName + "\' detected");
            if (s == 0) throw new Exception("Error: No \'" + propertyName + "\' detected");       
        }

        public void AssignVariable(string variableName, VariableType value)
        {
            if (isScreenSpace && value != VariableType.XYScreenCoordinates) throw new Exception("Screenspace shaders only support XY screen Position");

            int s = 0;

            for (int i = 0; i < FSData.Length; i++)
                if (FSData[i].Name == variableName)
                {
                    if (Attribute.IsDefined(FSData[i], typeof(Structs.variable)))
                    {
                        if (Marshal.SizeOf(FSData[i].FieldType) != variableTypeToSize(value))
                            throw new Exception("\'" + FSData[i].Name + "\' is not the appropriate type for \'" + value.ToString() + "\'");

                        FSD[i].Size = variableTypeToSize(value);
                        FSD[i].bytePosition = (int)Marshal.OffsetOf(FSStruct, variableName);

                        FSD[i].CustomInOutUniform = (int)DataItemType.Variable;
                        FSD[i].AssignedValue = (int)value;
                        s++;
                    }
                    else throw new Exception("You cannot AssignVariable() to a non variable type");
                }

            if (s >= 2) throw new Exception("Error: Multiple \'" + variableName + "\' detected");
            if (s == 0) throw new Exception("Error: No \'" + variableName + "\' detected");

        }

        int variableTypeToSize(VariableType value)
        {
            if (value == VariableType.Depth) return 4;
            else if (value == VariableType.XYScreenCoordinates) return 8;
            else if (value == VariableType.XYZCameraSpace) return 12;
            else if (value == VariableType.FaceIndex) return 4;
            else throw new Exception("Invalid Value");
        }
    }

    struct DataItem
    {
        public int Size;
        public int bytePosition;
        public int CustomInOutUniform;
        public GLBuffer AssignedBuffer;
        public GLTexture AssignedTexture;
        public object AssignedValue;
    }

    enum DataItemType
    { 
        Uniform = 0,
        In = 1,
        Out = 2,
        Variable = 3
    }

    public enum VariableType
    { 
        XYScreenCoordinates = 0,
        XYZCameraSpace = 1,
        Depth = 2,
        FaceIndex = 3
    }
}

namespace xfcore.Shaders.Structs
{
#pragma warning disable 0169
    struct vec3
    {
        float x;
        float y;
        float z;
    }

    struct vec4
    {
        float x;
        float y;
        float z;
        float w;
    }

    struct vec2
    {
        float x;
        float y;
    }

    unsafe struct sampler2D
    {
        int w;
        int h;
        int* TEXTURE_ADDR;

        public sampler2D(int W, int H, int* A)
        {
            w = W;
            h = H;
            TEXTURE_ADDR = A;
        }
    }

    struct byte4
    {
        byte B;
        byte G;
        byte R;
        byte A;
    }

    struct int2
    {
        int X;
        int Y;           
    }

#pragma warning restore 0169

    public class uniform : Attribute
    {
    }

    public class xinp : Attribute
    {
    }

    public static class Layout
    {
        public class Location : Attribute
        {
            public Location(int location)
            {

            }
        }
    }

    public class VS_out : Attribute
    {
    }

    public class xout : Attribute
    {
    }

    public class variable : Attribute
    {
    }

}