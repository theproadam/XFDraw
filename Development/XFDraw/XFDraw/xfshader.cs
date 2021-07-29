using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using xfcore.Buffers;
using xfcore.Shaders.Builder;
using xfcore.Extras;

namespace xfcore.Shaders
{
    public unsafe class Shader
    {
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
        static extern int LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
        static extern IntPtr GetProcAddress(int hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        static extern bool FreeLibrary(int hModule);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void ShdrScrnCallDel(int Width, int Height, byte** ptrPtrs, void* UniformPointer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ShdrCallDel(int start, int stop, float* tris, float* dptr, byte* uData1, byte* uData2, byte** ptrPtrs, GLData pData, int FACE, int mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int SafetyChkDel();

        int hModule;

        ShdrScrnCallDel ShaderCallScreen;
        internal ShdrCallDel ShaderCall;
        ShaderCompile shaderData;

        ShaderField[] uniformVS;
        ShaderField[] uniformFS;

        ShaderField[] fieldVSIn;
        ShaderField[] fieldFSIn;

        ShaderField[] fieldVSOut;
        ShaderField[] fieldFSOut;

        byte[] uniformBytesVS;
        byte[] uniformBytesFS;

        GLTexture[] textureSlots;

        internal int readStride;
        internal int intStride;

        internal bool isScreenSpace = false;

        internal object ThreadLock = new object();

        static void fRtlZeroMem(byte* ptr, int size)
        {
            for (int i = 0; i < size; i++)
                ptr[i] = 0;
        }

        public Shader(string shaderDLL, ShaderCompile sModule)
	    {
            if (!File.Exists(shaderDLL))
                throw new FileNotFoundException("File not found!");

            hModule = LoadLibrary(shaderDLL);
            if (hModule == 0) throw new Exception("Could not load shader: hModule from LoadLibrary() is zero!");

            IntPtr SafetyCheckTrigger = GetProcAddress(hModule, "CheckSize");
            IntPtr ShaderCallTrigger = GetProcAddress(hModule, "ShaderCallFunction");

            if (SafetyCheckTrigger == IntPtr.Zero)
                throw new Exception("Could not find shader size check function!");

            if (ShaderCallTrigger == IntPtr.Zero)
                throw new Exception("Could not find shader \"ShaderCallFunction()\" entry point!");

            SafetyChkDel GetShaderData = (SafetyChkDel)Marshal.GetDelegateForFunctionPointer(SafetyCheckTrigger, typeof(SafetyChkDel));

            int ptrSize = GetShaderData();

            isScreenSpace = sModule.GetScreenSpace();

            if (ptrSize != 4)
                throw new Exception("XFDraw only supports shaders compiled for 32 bit systems!");
           
            if (isScreenSpace)
                ShaderCallScreen = (ShdrScrnCallDel)Marshal.GetDelegateForFunctionPointer(ShaderCallTrigger, typeof(ShdrScrnCallDel));
            else
                ShaderCall = (ShdrCallDel)Marshal.GetDelegateForFunctionPointer(ShaderCallTrigger, typeof(ShdrCallDel));

            shaderData = sModule;

            int sizeValue;
            uniformFS = shaderData.PrepareUniformsFS(out sizeValue);
            uniformBytesFS = new byte[sizeValue];

            if (!isScreenSpace)
            {
                uniformVS = shaderData.PrepareUniformsVS(out sizeValue);
                uniformBytesVS = new byte[sizeValue];

                fieldVSIn = shaderData.sFieldsInVS;
                fieldVSOut = shaderData.sFieldsOutVS;

                readStride = shaderData.readStride;
                intStride = shaderData.inteStride;
            }

            fieldFSIn = shaderData.sFieldsInFS;
            fieldFSOut = shaderData.sFieldsOutFS;

            if (isScreenSpace)
                textureSlots = new GLTexture[fieldFSIn.Length + fieldFSOut.Length];
            else 
                textureSlots = new GLTexture[fieldFSOut.Length];
	    }

        public void SetValue(string uniformName, object value)
        {
            int setCount = 0;
            bool isStruct = false;
            string structFind = "";

            if (!value.GetType().IsValueType || value.GetType().IsEnum)
                throw new Exception("Value must be a struct!");

            if (uniformName.Contains('.')){
                structFind = uniformName.Split('.')[1];
                uniformName = uniformName.Split('.')[0];
                isStruct = true;
                
            }

            if (!isScreenSpace)
            for (int i = 0; i < uniformVS.Length; i++)
            {
                if (uniformVS[i].name == uniformName)
                {
                    if (uniformVS[i].GetSize() == -1) throw new Exception("An error occured (12852)");

                    if (isStruct) uniformVS[i].typeAlt.SetValue(structFind, uniformVS[i].layoutPosition, uniformBytesVS, value);
                    else
                    {
                        int mSize = Marshal.SizeOf(value);
                        if (uniformVS[i].GetSize() != mSize)
                            throw new Exception("\"" + uniformName + "\" is not the same size as the value!");

                        GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
                        byte* ptr = (byte*)handle.AddrOfPinnedObject();

                        for (int n = 0; n < mSize; n++)
                            uniformBytesVS[uniformVS[i].layoutPosition + n] = ptr[n];

                        handle.Free();
                    }
                    setCount++;
                }
            }

            for (int i = 0; i < uniformFS.Length; i++)
            {
                if (uniformFS[i].name == uniformName)
                {
                    if (uniformFS[i].GetSize() == -1) throw new Exception("An error occured (12852)");

                    if (isStruct) uniformFS[i].typeAlt.SetValue(structFind, uniformFS[i].layoutPosition, uniformBytesFS, value);
                    else
                    {
                        int mSize = Marshal.SizeOf(value);
                        if (uniformFS[i].GetSize() != mSize)
                            throw new Exception("\"" + uniformName + "\" is not the same size as the value!");

                        GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
                        byte* ptr = (byte*)handle.AddrOfPinnedObject();

                        for (int n = 0; n < mSize; n++)
                            uniformBytesFS[uniformFS[i].layoutPosition + n] = ptr[n];

                        handle.Free();
                    }
                    setCount++;
                }
            }


            if (setCount == 0)
                throw new Exception("Uniform \"" + uniformName + "\" was not found in the shader!");
            else if (setCount >= 2)
                throw new Exception("Uniform \"" + uniformName + "\" was found multiple times in the shader!");
        }

        public void AssignBuffer(string bufferName, GLTexture buffer)
        {
            int setCount = 0;

            for (int i = 0; i < fieldFSOut.Length; i++)
            {
                if (fieldFSOut[i].name == bufferName)
                {
                    if (buffer.Stride != fieldFSOut[i].GetSize())
                        throw new Exception("\"" + fieldFSOut[i].typeName + "\" is not the same size (" +
                            fieldFSOut[i].GetSize() + ") as the buffer stride! (" + buffer.Stride + ")");

                    textureSlots[i] = buffer;
                    setCount++;
                }
            }

            if (isScreenSpace) 
            { 
                for (int i = 0; i < fieldFSIn.Length; i++)
                {
                    if (fieldFSIn[i].name == bufferName)
                    {
                        if (buffer.Stride != fieldFSIn[i].GetSize())
                            throw new Exception("\"" + fieldFSIn[i].typeName + "\" is not the same size (" +
                                fieldFSIn[i].GetSize() + ") as the buffer stride! (" + buffer.Stride + ")");

                        textureSlots[i] = buffer;
                        setCount++;
                    }
                }
            }

            if (setCount == 0) throw new Exception("Buffer \"" + bufferName + "\" was not found in the shader!");
            else if (setCount >= 2) throw new Exception("Buffer \"" + bufferName + "\" was found multiple times in the shader!");
        }

        public void Pass()
        {
            if (!isScreenSpace)
                throw new Exception("Pass() can only be used with screenspace shaders!");

            //Prevent a user from triggering the shader in a multithread environment
            lock (ThreadLock)
            {
                for (int i = 0; i < textureSlots.Length; i++)
                    if (textureSlots[i] == null)
                        throw new Exception("One of the assigned textures is null!");

                if (textureSlots.Length == 0)
                    throw new Exception("Atleast one Buffer must be assigned!");

                for (int i = 0; i < textureSlots.Length; i++)
                    textureSlots[i].RequestLock();

                int width = textureSlots[0].Width, height = textureSlots[0].Height;

                for (int i = 1; i < textureSlots.Length; i++){
                    if (textureSlots[i].Height != height) throw new Exception("Height must be the same on all buffers!");
                    if (textureSlots[i].Width != width) throw new Exception("Width must be the same on all buffers!");
                }

                IntPtr ptrPtrs = Marshal.AllocHGlobal(textureSlots.Length * 4);
                GCHandle uniformData = GCHandle.Alloc(uniformBytesFS, GCHandleType.Pinned);
                byte* bptr = (byte*)ptrPtrs;

                fRtlZeroMem(bptr, textureSlots.Length * 4); //Zero it just in case something is wrong, so it crashes instantly.

                byte** PTRS = (byte**)bptr;
                for (int i = 0; i < textureSlots.Length; i++) 
                    PTRS[i] = (byte*)textureSlots[i].GetAddress();

                //Call the shader
                ShaderCallScreen(width, height, PTRS, (void*)uniformData.AddrOfPinnedObject());

                Marshal.FreeHGlobal(ptrPtrs);
                uniformData.Free();

                for (int i = 0; i < textureSlots.Length; i++)
                    textureSlots[i].ReleaseLock();
            }
        }

        internal GLTexture[] GetTextureSlots()
        {
            return textureSlots;
        }

        internal byte[] GetUniformVS()
        {
            return uniformBytesVS;
        }

        internal byte[] GetUniformFS()
        {
            return uniformBytesFS;
        }

        public void FreeLibrary()
        {
            FreeLibrary(hModule);
        }

        public Shader DuplicateShader()
        {
            throw new Exception();
        }
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

        internal GLData(int rWidth, int rHeight, GLMatrix proj)
        {
            if (proj.ZNear <= 0) throw new Exception("Invalid ZNear!");
            if (proj.ZFar <= 0) throw new Exception("Invalid ZFar");
            if (proj.ZNear >= proj.ZFar) throw new Exception("Invalid ZNear ZFar");

            const float deg2rad = (float)(Math.PI / 180d);

            matrixlerpv = proj.iValue;

            nearZ = proj.ZNear;
            farZ = proj.ZFar;

            renderWidth = rWidth;
            renderHeight = rHeight;

            rw = ((float)rWidth - 1f) / 2f;
            rh = ((float)rHeight - 1f) / 2f;

            tanVert = (float)Math.Tan(deg2rad * proj.vFOV / 2.0f) * (1.0f - proj.iValue);
            tanHorz = (float)Math.Tan(deg2rad * proj.hFOV / 2.0f) * (1.0f - proj.iValue);

            fw = rw / (float)Math.Tan(deg2rad * proj.vFOV / 2.0f);
            fh = rh / (float)Math.Tan(deg2rad * proj.hFOV / 2.0f);

            ow = proj.vSize * proj.iValue;
            oh = proj.hSize * proj.iValue;

            ox = rw / (proj.vSize == 0 ? 1 : proj.vSize);
            oy = rh / (proj.hSize == 0 ? 1 : proj.hSize);

            iox = 1f / ox;
            ioy = 1f / oy;

            oValue = ow / (float)Math.Tan(proj.vFOV / 2f) * (1f - matrixlerpv);
            
        }
    }
}

  