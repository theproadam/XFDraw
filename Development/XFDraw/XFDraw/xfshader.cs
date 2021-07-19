using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using xfcore.Buffers;
using xfcore.Shaders.Parser;
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
        delegate void ShdrCallDel();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int SafetyChkDel();

        ShdrScrnCallDel ShaderCallScreen;
        ShaderCompile shaderData;

        ShaderField[] uniformVS;
        ShaderField[] uniformFS;

        ShaderField[] fieldVS;
        ShaderField[] fieldFS;

        byte[] uniformBytesVS;
        byte[] uniformBytesFS;

        GLTexture[] textureSlots;

        object ThreadLock = new object();

        static void fRtlZeroMem(byte* ptr, int size)
        {
            for (int i = 0; i < size; i++)
                ptr[i] = 0;
        }

        public Shader(string shaderDLL, ShaderCompile sModule)
	    {
            if (!File.Exists(shaderDLL))
                throw new FileNotFoundException("File not found!");

            int hModule = LoadLibrary(shaderDLL);
            if (hModule == 0) throw new Exception("Could not load shader: hModule from LoadLibrary() is zero!");

            IntPtr SafetyCheckTrigger = GetProcAddress(hModule, "CheckSize");
            IntPtr ShaderCallTrigger = GetProcAddress(hModule, "ShaderCallFunction");

            if (SafetyCheckTrigger == IntPtr.Zero)
                throw new Exception("Could not find shader size check function!");

            if (ShaderCallTrigger == IntPtr.Zero)
                throw new Exception("Could not find shader \"ShaderCallFunction()\" entry point!");

            SafetyChkDel GetShaderData = (SafetyChkDel)Marshal.GetDelegateForFunctionPointer(SafetyCheckTrigger, typeof(SafetyChkDel));

            int ptrSize = GetShaderData();

            if (ptrSize != 4)
                throw new Exception("XFDraw only supports shaders compiled for 32 bit systems!");

            ShaderCallScreen = (ShdrScrnCallDel)Marshal.GetDelegateForFunctionPointer(ShaderCallTrigger, typeof(ShdrScrnCallDel));
            shaderData = sModule;

            int sizeValue;
            uniformFS = shaderData.PrepareUniformsFS(out sizeValue);
            uniformBytesFS = new byte[sizeValue];

          //  uniformVS = shaderData.PrepareUniformsVS(out sizeValue);
          //  uniformBytesVS = new byte[sizeValue];

            fieldFS = shaderData.PrepareFieldFS();
            textureSlots = new GLTexture[fieldFS.Length];
	    }

        public void SetValue(string uniformName, object value)
        {
            int setCount = 0;

            if (value.GetType() == typeof(Vector2))
            {
                for (int i = 0; i < uniformFS.Length; i++)
                {
                    if (uniformFS[i].name == uniformName)
                    {
                        if (uniformFS[i].dataType != DataType.vec2)
                            throw new Exception("Uniform \"" + uniformName + "\" is not a Vector2!");

                        Vector2 vec2 = (Vector2)value;
                        byte* ptr = (byte*)&vec2;

                        for (int n = 0; n < 8; n++)
                            uniformBytesFS[uniformFS[i].layoutPosition + n] = ptr[n];

                        setCount++;
                    }
                }
            }
            else if (value.GetType() == typeof(int) || value.GetType() == typeof(Color4))
            {
                for (int i = 0; i < uniformFS.Length; i++)
                {
                    if (uniformFS[i].name == uniformName)
                    {
                        if (uniformFS[i].dataType != DataType.int32)
                            throw new Exception("Uniform \"" + uniformName + "\" is not a int!");

                        int rawValue = (int)value;
                        byte* ptr = (byte*)&rawValue;

                        for (int n = 0; n < 4; n++)
                            uniformBytesFS[uniformFS[i].layoutPosition + n] = ptr[n];

                        setCount++;
                    }
                }
            }

            else throw new Exception();




            if (setCount == 0)
                throw new Exception("Uniform \"" + uniformName + "\" was not found in the shader!");
            else if (setCount >= 2)
                throw new Exception("Uniform \"" + uniformName + "\" was found multiple times in the shader!");
        }

        public void AssignBuffer(string bufferName, GLTexture buffer)
        {
            int setCount = 0;

            for (int i = 0; i < fieldFS.Length; i++)
            {
                if (fieldFS[i].name == bufferName)
                {
                    if (buffer.Stride != fieldFS[i].GetSize())
                        throw new Exception("\"" + fieldFS[i].typeName + "\" is not the same size (" +
                            fieldFS[i].GetSize() + ") as the buffer stride! (" + buffer.Stride + ")");

                    textureSlots[i] = buffer;
                    setCount++;
                }
            }

            if (setCount == 0) throw new Exception("Buffer \"" + bufferName + "\" was not found in the shader!");
            else if (setCount >= 2) throw new Exception("Buffer \"" + bufferName + "\" was found multiple times in the shader!");
        }

        public void Pass()
        {
            //Prevent a user from triggering the shader in a multithread environment
            lock (ThreadLock)
            {
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


        public Shader DuplicateShader()
        {
            return null;
        }
    }
}

  